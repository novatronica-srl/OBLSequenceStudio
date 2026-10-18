#nullable disable
using System.IO.Ports;

namespace OBLSequenceStudio.STBootLib
{
    /// <summary>Implements the STM32 serial bootloader command protocol over an owned serial port.</summary>
    public class STBoot : IDisposable
    {
        /* serial port */
        SerialPort sp;
        /* command mutex */
        SemaphoreSlim sem;
        /* list of supported commands */
        List<STCmds> Commands;
        private bool disposed;


        /* bootloader version */
        /// <summary>Bootloader version populated by the initialization handshake.</summary>
        public string Version;
        /* product id */
        /// <summary>Device product identifier populated by the GET_ID command.</summary>
        public ushort ProductID;
        

        /* constructor */
        /// <summary>Creates a command gate; opening the serial port is a separate operation.</summary>
        public STBoot()
        {
            Commands = new List<STCmds>();
            /* initialize mutex */
            sem = new SemaphoreSlim(1, 1);
        }

        /* dispose implementation */
        /// <summary>Closes the owned port and releases the command semaphore once.</summary>
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            /* close serial port */
            Close();
            sem.Dispose();
        }

        /* open serial port */
        /// <summary>Opens an 8E1 serial connection with one-second read and write timeouts and clears its buffers.</summary>
        /// <param name="portName">Serial port of a board already started in its bootloader.</param>
        /// <param name="baudRate">Serial bit rate used for bootloader autobaud.</param>
        public void Open(string portName, uint baudRate)
        {
            /* initialize serial port */
            sp = new SerialPort(portName, (int)baudRate, Parity.Even, 8, StopBits.One) { ReadTimeout = 1000, WriteTimeout = 1000 };
            /* open serial port */
            sp.Open();

            /* discard buffers */
            sp.DiscardInBuffer();
            sp.DiscardOutBuffer();
        }

        /* close */
        /// <summary>Closes the owned serial port without disposing the command gate.</summary>
        public void Close()
        {
            /* close permitted? */
            sp?.Dispose(); sp = null;
        }

        /* initialize communication */
        /// <summary>Performs autobaud, reads supported commands and version, and discovers the product identifier.</summary>
        public async Task Initialize()
        {
            /* perform autobauding */
            await Init().ConfigureAwait(false);
            /* get version and command list */
            await Get().ConfigureAwait(false);

            /* no support for get id? */
            if (!Commands.Contains(STCmds.GET_ID)) {
                /* throw an exception */
                throw new STBootException("Command not supported");
            }

            /* get product id */
            await GetID().ConfigureAwait(false);
        }

        /* unprotect memory */
        /// <summary>Requests read and write unprotection when supported; these commands may erase memory and reset the target.</summary>
        public async Task Unprotect()
        {
            /* no support for unprotect? */
            if (!Commands.Contains(STCmds.WR_UNPROTECT))
                throw new STBootException("Command not supported");

            /* no support for unprotect? */
            if (!Commands.Contains(STCmds.RD_UNPROTECT))
                throw new STBootException("Command not supported");

            await ReadUnprotect().ConfigureAwait(false);
            await WriteUnprotect().ConfigureAwait(false);
        }

        /* read memory */
        /// <summary>Reads memory in chunks of at most 256 bytes, observing cancellation between chunks.</summary>
        /// <param name="address">Initial target memory address.</param>
        /// <param name="buf">Destination buffer.</param>
        /// <param name="offset">Initial destination offset in bytes.</param>
        /// <param name="size">Number of bytes to read.</param>
        /// <param name="p">Optional cumulative byte progress.</param>
        /// <param name="ct">Cancellation checked between chunk operations.</param>
        public async Task ReadMemory(uint address, byte[] buf, int offset, 
            int size, IProgress<int> p, CancellationToken ct)
        {
            /* number of bytes read */
            int bread = 0;
            /* no support for read? */
            if (!Commands.Contains(STCmds.READ))
                throw new STBootException("Command not supported");

            /* data is read in chunks */
            while (size > 0 && !ct.IsCancellationRequested) {
                /* chunk size */
                int csize = Math.Min(size, 256);
                /* read a single chunk */
                await Read(address, buf, offset, csize).ConfigureAwait(false);
         
                /* update iterators */
                size -= csize; offset += csize; address += (uint)csize; 
                /* update number of bytes read */
                bread += csize;

                /* report progress */
                if (p != null)
                    p.Report(bread);
            }

            /* throw exception if operation was cancelled */
            if (ct.IsCancellationRequested)
                throw new OperationCanceledException("Read cancelled");
        }

        /* write memory */
        /// <summary>Writes memory in chunks of at most 256 bytes with four-byte address and length alignment.</summary>
        /// <param name="address">Word-aligned target memory address.</param>
        /// <param name="buf">Source buffer, including any required alignment padding.</param>
        /// <param name="offset">Initial source offset in bytes.</param>
        /// <param name="size">Positive multiple of four bytes to write.</param>
        /// <param name="p">Optional processed and total byte progress.</param>
        /// <param name="ct">Cancellation checked between chunk operations.</param>
        /// <exception cref="ArgumentOutOfRangeException">Alignment, length or source-buffer bounds are invalid.</exception>
        public async Task WriteMemory(uint address, byte[] buf, int offset,
            int size, IProgress<STBootProgress> p, CancellationToken ct)
        {
            if ((address & 3) != 0 || (size & 3) != 0 || size <= 0 || offset < 0 || offset + size > buf.Length)
                throw new ArgumentOutOfRangeException(nameof(size));
            /* number of bytes written */
            int bwritten = 0, btotal = size;

            /* no support for read? */
            if (!Commands.Contains(STCmds.WRITE))
                throw new STBootException("Command not supported");

            /* data is read in chunks */
            while (size > 0 && !ct.IsCancellationRequested) {
                /* chunk size */
                int csize = Math.Min(size, 256);
                /* read a single chunk */
                await Write(address, buf, offset, csize).ConfigureAwait(false);

                /* update iterators */
                size -= csize; offset += csize; address += (uint)csize;
                /* update number of bytes read */
                bwritten += csize;

                /* report progress */
                if (p != null)
                    p.Report(new STBootProgress(bwritten, btotal));
            }

            /* throw exception if operation was cancelled */
            if (ct.IsCancellationRequested)
                throw new OperationCanceledException("Write cancelled");
        }

        /* erase page */
        /// <summary>Erases one page using the classic or extended command supported by the bootloader.</summary>
        public async Task ErasePage(uint pageNumber)
        {
            /* 'classic' erase operation supported? */
            if (Commands.Contains(STCmds.ERASE)) {
                await Erase(pageNumber).ConfigureAwait(false);
            /* 'extended' erase operation supported? */
            } else if (Commands.Contains(STCmds.EXT_ERASE)) {
                await ExtendedErase(pageNumber).ConfigureAwait(false);
            /* no operation supported */
            } else {
                throw new STBootException("Command not supported");
            }
        }

        /* perform global erase */
        /// <summary>Erases all flash using the supported special erase sequence and its longer acknowledgement timeout.</summary>
        public async Task GlobalErase()
        {
            /* 'classic' erase operation supported? */
            if (Commands.Contains(STCmds.ERASE)) {
                await EraseSpecial(STEraseMode.GLOBAL).ConfigureAwait(false);
                /* 'extended' erase operation supported? */
            } else if (Commands.Contains(STCmds.EXT_ERASE)) {
                await ExtendedEraseSpecial(STExtendedEraseMode.GLOBAL).ConfigureAwait(false);
                /* no operation supported */
            } else {
                throw new STBootException("Command not supported");
            }
        }

        /* jump to user code */
        /// <summary>Requests execution at the target address when the bootloader supports GO.</summary>
        public async Task Jump(uint address)
        {
            /* no support for go? */
            if (!Commands.Contains(STCmds.GO))
                throw new STBootException("Command not supported");

            /* go! */
            await Go(address).ConfigureAwait(false);
        }

        /* init */
        private async Task Init()
        {
            /* command word */
            var tx = new byte[1];
            /* response code */
            var ack = new byte[1];

            /* store code */
            tx[0] = (byte)STCmds.INIT;

            /* wait for command sender to finish its job with previous 
             * command */
            await sem.WaitAsync().ConfigureAwait(false);

            /* try to send command and wait for response */
            try {
                /* send bytes */
                await SerialWrite(tx, 0, tx.Length).ConfigureAwait(false);

                /* wait for response code */
                await SerialRead(ack, 0, 1).ConfigureAwait(false);
               /* check response code */
                if (ack[0] != (byte)STResps.ACK)
                    throw new STBootException("Command Rejected");
            /* error during send */
            } catch (Exception) {
                /* release semaphore */
                sem.Release();
                /* re-throw */
                throw;
            }

            /* release semaphore */
            sem.Release();
        }

        /* get command */
        private async Task Get()
        {
            /* command word */
            var tx = new byte[2];
            /* temporary storage for response bytes */
            var tmp = new byte[1];
            /* numbe or response bytes */
            int nbytes;
            /* rx buffer */
            byte[] rx;
           
            /* store code */
            tx[0] = (byte)STCmds.GET;
            /* set checksum */
            tx[1] = ComputeChecksum(tx, 0, 1);

            /* wait for command sender to finish its job with previous 
             * command */
            await sem.WaitAsync().ConfigureAwait(false);

            /* try to send command and wait for response */
            try {
                /* send bytes */
                await SerialWrite(tx, 0, tx.Length).ConfigureAwait(false);

                /* wait for response code */
                await SerialRead(tmp, 0, 1).ConfigureAwait(false);
                /* check response code */
                if (tmp[0] != (byte)STResps.ACK) 
                    throw new STBootException("Command Rejected");

                /* wait for number of bytes */
                await SerialRead(tmp, 0, 1).ConfigureAwait(false);
                /* assign number of bytes that will follow (add for acks) */
                nbytes = tmp[0] + 2;
                /* GET length depends on supported bootloader commands. */
                if (nbytes < 2 || nbytes > 258)
                    throw new STBootException("Invalid length");

                /* prepare buffer */
                rx = new byte[nbytes];
                /* receive response */
                await SerialRead(rx, 0, rx.Length).ConfigureAwait(false);
                if (rx[rx.Length - 1] != (byte)STResps.ACK) throw new STBootException("Invalid ACK");
            /* oops, something baaad happened! */
            } catch (Exception) {
                /* release semaphore */
                sem.Release();
                /* re-throw */
                throw;
            }

            /* store version information */
            Version = (rx[0] >> 4).ToString() + "." + 
                (rx[0] & 0xf).ToString();

            /* initialize command list */
            Commands = new List<STCmds>();
            /* add all commands */
            for (int i = 1; i < nbytes - 1; i++)
                Commands.Add((STCmds)rx[i]);

            /* release semaphore */
            sem.Release();
        }

        /* get id command */
        private async Task GetID()
        {
            /* command word */
            var tx = new byte[2];
            /* temporary storage for response bytes */
            var tmp = new byte[1];
            /* numbe or response bytes */
            int nbytes;
            /* rx buffer */
            byte[] rx;

            /* store code */
            tx[0] = (byte)STCmds.GET_ID;
            /* set checksum */
            tx[1] = ComputeChecksum(tx, 0, 1);

            /* try to send command and wait for response */
            await sem.WaitAsync().ConfigureAwait(false);
            try {
                /* send bytes */
                await SerialWrite(tx, 0, tx.Length).ConfigureAwait(false);

                /* wait for response code */
                await SerialRead(tmp, 0, 1).ConfigureAwait(false);
                /* check response code */
                if (tmp[0] != (byte)STResps.ACK)
                    throw new STBootException("Command Rejected");

                /* wait for number of bytes */
                await SerialRead(tmp, 0, 1).ConfigureAwait(false);
                /* assign number of bytes that will follow (add for acks) */
                nbytes = tmp[0] + 2;
                /* nbytes must be equal to 3 for stm32 products */
                if (nbytes != 3)
                    throw new STBootException("Invalid length");

                /* prepare buffer */
                rx = new byte[nbytes];
                /* receive response */
                await SerialRead(rx, 0, rx.Length).ConfigureAwait(false);
                if (rx[rx.Length - 1] != (byte)STResps.ACK) throw new STBootException("Invalid ACK");
            /* oops, something baaad happened! */
            } catch (Exception) {
                /* release semaphore */
                sem.Release();
                /* re-throw */
                throw;
            }

            /* store product id */
            ProductID = (ushort)(rx[0] << 8 | rx[1]);

            /* release semaphore */
            sem.Release();
        }

        /* read command */
        private async Task Read(uint address, byte[] buf, int offset, int length)
        {
            /* command word */
            var tx = new byte[9];
            /* temporary storage for response bytes */
            var tmp = new byte[1];

            /* command code */
            tx[0] = (byte)STCmds.READ;
            /* checksum */
            tx[1] = ComputeChecksum(tx, 0, 1);

            /* store address */
            tx[2] = (byte)((address >> 24) & 0xff);
            tx[3] = (byte)((address >> 16) & 0xff);
            tx[4] = (byte)((address >> 8) & 0xff);
            tx[5] = (byte)(address & 0xff);
            /* address checksum (needs to be not negated. why? because ST! 
             * that's why. */
            tx[6] = (byte)~ComputeChecksum(tx, 2, 4);

            /* store number of bytes */
            tx[7] = (byte)(length - 1);
            /* size checksum */
            tx[8] = ComputeChecksum(tx, 7, 1);

            /* try to send command and wait for response */
            await sem.WaitAsync().ConfigureAwait(false);
            try {
                /* send bytes */
                await SerialWrite(tx, 0, 2).ConfigureAwait(false);
                /* wait for response code */
                await SerialRead(tmp, 0, 1).ConfigureAwait(false);
                /* check response code */
                if (tmp[0] != (byte)STResps.ACK)
                    throw new STBootException("Command Rejected");

                /* send address */
                await SerialWrite(tx, 2, 5).ConfigureAwait(false);
                /* wait for response code */
                await SerialRead(tmp, 0, 1).ConfigureAwait(false);
                /* check response code */
                if (tmp[0] != (byte)STResps.ACK)
                    throw new STBootException("Address Rejected");

                /* send address */
                await SerialWrite(tx, 7, 2).ConfigureAwait(false);
                /* wait for response code */
                await SerialRead(tmp, 0, 1).ConfigureAwait(false);
                /* check response code */
                if (tmp[0] != (byte)STResps.ACK)
                    throw new STBootException("Size Rejected");

                /* receive response */
                await SerialRead(buf, offset, length).ConfigureAwait(false);
                /* oops, something baaad happened! */
            } catch (Exception) {
                /* release semaphore */
                sem.Release();
                /* re-throw */
                throw;
            }

            /* release semaphore */
            sem.Release();
        }

        /* go command */
        private async Task Go(uint address)
        {
            /* command word */
            var tx = new byte[7];
            /* temporary storage for response bytes */
            var tmp = new byte[1];

            /* command code */
            tx[0] = (byte)STCmds.GO;
            /* checksum */
            tx[1] = ComputeChecksum(tx, 0, 1);

            /* store address */
            tx[2] = (byte)((address >> 24) & 0xff);
            tx[3] = (byte)((address >> 16) & 0xff);
            tx[4] = (byte)((address >> 8) & 0xff);
            tx[5] = (byte)(address & 0xff);
            /* address checksum (needs to be not negated. why? because ST! 
             * that's why. */
            tx[6] = (byte)~ComputeChecksum(tx, 2, 4);

            /* try to send command and wait for response */
            await sem.WaitAsync().ConfigureAwait(false);
            try {
                /* send bytes */
                await SerialWrite(tx, 0, 2).ConfigureAwait(false);
                /* wait for response code */
                await SerialRead(tmp, 0, 1).ConfigureAwait(false);
                /* check response code */
                if (tmp[0] != (byte)STResps.ACK)
                    throw new STBootException("Command Rejected");

                /* send address */
                await SerialWrite(tx, 2, 5).ConfigureAwait(false);
                /* wait for response code */
                await SerialRead(tmp, 0, 1).ConfigureAwait(false);
                /* check response code */
                if (tmp[0] != (byte)STResps.ACK)
                    throw new STBootException("Address Rejected");
                /* oops, something baaad happened! */
            } catch (Exception) {
                /* release semaphore */
                sem.Release();
                /* re-throw */
                throw;
            }

            /* release semaphore */
            sem.Release();
        }

        /* write memory */
        private async Task Write(uint address, byte[] data, int offset, int length)
        {
            /* command word */
            var tx = new byte[9];
            /* temporary storage for response bytes */
            var tmp = new byte[1];

            /* command code */
            tx[0] = (byte)STCmds.WRITE;
            /* checksum */
            tx[1] = ComputeChecksum(tx, 0, 1);

            /* store address */
            tx[2] = (byte)((address >> 24) & 0xff);
            tx[3] = (byte)((address >> 16) & 0xff);
            tx[4] = (byte)((address >> 8) & 0xff);
            tx[5] = (byte)(address & 0xff);
            /* address checksum (needs to be not negated. why? because ST! 
             * that's why. */
            tx[6] = (byte)~ComputeChecksum(tx, 2, 4);

            /* number of bytes */
            tx[7] = (byte)(length - 1);
            /* data checksum */
            tx[8] = (byte)(~(ComputeChecksum(data, offset, length) ^ tx[7]));

            /* try to send command and wait for response */
            await sem.WaitAsync().ConfigureAwait(false);
            try {
                /* send bytes */
                await SerialWrite(tx, 0, 2).ConfigureAwait(false);
                /* wait for response code */
                await SerialRead(tmp, 0, 1).ConfigureAwait(false);
                /* check response code */
                if (tmp[0] != (byte)STResps.ACK)
                    throw new STBootException("Command Rejected");

                /* send address */
                await SerialWrite(tx, 2, 5).ConfigureAwait(false);
                /* wait for response code */
                await SerialRead(tmp, 0, 1).ConfigureAwait(false);
                /* check response code */
                if (tmp[0] != (byte)STResps.ACK)
                    throw new STBootException("Address Rejected");

                /* Send length, data and checksum as one contiguous frame. */
                byte[] frame = new byte[length + 2];
                frame[0] = tx[7];
                Array.Copy(data, offset, frame, 1, length);
                frame[frame.Length - 1] = tx[8];
                await SerialWrite(frame, 0, frame.Length).ConfigureAwait(false);
                /* wait for response code */
                await SerialRead(tmp, 0, 1).ConfigureAwait(false);
                /* check response code */
                if (tmp[0] != (byte)STResps.ACK)
                    throw new STBootException("Data Rejected");

                /* oops, something baaad happened! */
            } catch (Exception) {
                /* release semaphore */
                sem.Release();
                /* re-throw */
                throw;
            }

            /* release semaphore */
            sem.Release();
        }

        /* erase memory page */
        private async Task Erase(uint pageNumber)
        {
            /* command word */
            var tx = new byte[5];
            /* temporary storage for response bytes */
            var tmp = new byte[1];

            /* command code */
            tx[0] = (byte)STCmds.ERASE;
            /* checksum */
            tx[1] = ComputeChecksum(tx, 0, 1);

            /* erase single page */
            tx[2] = 0;
            /* set page number */
            tx[3] = (byte)pageNumber;
            /* checksum */
            tx[4] = (byte)~ComputeChecksum(tx, 2, 2);

            /* try to send command and wait for response */
            await sem.WaitAsync().ConfigureAwait(false);
            try {
                /* send bytes */
                await SerialWrite(tx, 0, 2).ConfigureAwait(false);
                /* wait for response code */
                await SerialRead(tmp, 0, 1).ConfigureAwait(false);
                /* check response code */
                if (tmp[0] != (byte)STResps.ACK)
                    throw new STBootException("Command Rejected");

                /* send address */
                await SerialWrite(tx, 2, 3).ConfigureAwait(false);
                /* wait for response code */
                await SerialRead(tmp, 0, 1).ConfigureAwait(false);
                /* check response code */
                if (tmp[0] != (byte)STResps.ACK)
                    throw new STBootException("Page Rejected");

                /* oops, something baaad happened! */
            } catch (Exception) {
                /* release semaphore */
                sem.Release();
                /* re-throw */
                throw;
            }

            /* release semaphore */
            sem.Release();
        }

        /* erase memory page */
        private async Task EraseSpecial(STEraseMode mode)
        {
            /* command word */
            var tx = new byte[4];
            /* temporary storage for response bytes */
            var tmp = new byte[1];

            /* command code */
            tx[0] = (byte)STCmds.ERASE;
            /* checksum */
            tx[1] = ComputeChecksum(tx, 0, 1);

            /* erase single page */
            tx[2] = (byte)((int)mode);
            /* AN3155 mass erase is the special pair FF 00. */
            tx[3] = 0x00;

            /* try to send command and wait for response */
            await sem.WaitAsync().ConfigureAwait(false);
            try {
                /* send bytes */
                await SerialWrite(tx, 0, 2).ConfigureAwait(false);
                /* wait for response code */
                await SerialRead(tmp, 0, 1, 30000).ConfigureAwait(false);
                /* check response code */
                if (tmp[0] != (byte)STResps.ACK)
                    throw new STBootException("Command Rejected");

                /* send address */
                await SerialWrite(tx, 2, 2).ConfigureAwait(false);
                /* wait for response code */
                await SerialRead(tmp, 0, 1, 30000).ConfigureAwait(false);
                /* check response code */
                if (tmp[0] != (byte)STResps.ACK)
                    throw new STBootException("Special Code Rejected");

                /* oops, something baaad happened! */
            } catch (Exception) {
                /* release semaphore */
                sem.Release();
                /* re-throw */
                throw;
            }

            /* release semaphore */
            sem.Release();
        }

        /* extended erase memory page */
        private async Task ExtendedErase(uint pageNumber)
        {
            /* command word */
            var tx = new byte[7];
            /* temporary storage for response bytes */
            var tmp = new byte[1];

            /* command code */
            tx[0] = (byte)STCmds.EXT_ERASE;
            /* checksum */
            tx[1] = ComputeChecksum(tx, 0, 1);

            /* erase single page */
            tx[2] = 0;
            tx[3] = 0;
            /* set page number */
            tx[4] = (byte)(pageNumber >> 8);
            tx[5] = (byte)(pageNumber >> 0);
            /* checksum */
            tx[6] = (byte)~ComputeChecksum(tx, 2, 5);

            /* try to send command and wait for response */
            await sem.WaitAsync().ConfigureAwait(false);
            try {
                /* send bytes */
                await SerialWrite(tx, 0, 2).ConfigureAwait(false);
                /* wait for response code */
                await SerialRead(tmp, 0, 1).ConfigureAwait(false);
                /* check response code */
                if (tmp[0] != (byte)STResps.ACK)
                    throw new STBootException("Command Rejected");

                /* send address */
                await SerialWrite(tx, 2, 5).ConfigureAwait(false);
                /* wait for response code. use longer timeout, erase might
                 * take a while or two. */
                await SerialRead(tmp, 0, 1, 3000).ConfigureAwait(false);
                /* check response code */
                if (tmp[0] != (byte)STResps.ACK) 
                    throw new STBootException("Page Rejected");

            /* oops, something baaad happened! */
            } catch (Exception) {
                /* release semaphore */
                sem.Release();
                /* re-throw */
                throw;
            }

            /* release semaphore */
            sem.Release();
        }

        /* extended erase memory page */
        private async Task ExtendedEraseSpecial(STExtendedEraseMode mode)
        {
            /* command word */
            var tx = new byte[5];
            /* temporary storage for response bytes */
            var tmp = new byte[1];

            /* command code */
            tx[0] = (byte)STCmds.EXT_ERASE;
            /* checksum */
            tx[1] = ComputeChecksum(tx, 0, 1);

            /* erase single page */
            tx[2] = (byte)((int)mode >> 8);
            tx[3] = (byte)((int)mode >> 0);
            /* checksum */
            tx[4] = (byte)~ComputeChecksum(tx, 2, 3);

            /* try to send command and wait for response */
            await sem.WaitAsync().ConfigureAwait(false);
            try {
                /* send bytes */
                await SerialWrite(tx, 0, 2).ConfigureAwait(false);
                /* wait for response code */
                await SerialRead(tmp, 0, 1).ConfigureAwait(false);
                /* check response code */
                if (tmp[0] != (byte)STResps.ACK)
                    throw new STBootException("Command Rejected");

                /* send address */
                await SerialWrite(tx, 2, 3).ConfigureAwait(false);
                /* wait for response code. use longer timeout, erase might
                 * take a while or two. */
                await SerialRead(tmp, 0, 1, 30000).ConfigureAwait(false);
                /* check response code */
                if (tmp[0] != (byte)STResps.ACK)
                    throw new STBootException("Special code Rejected");

                /* oops, something baaad happened! */
            } catch (Exception) {
                /* release semaphore */
                sem.Release();
                /* re-throw */
                throw;
            }

            /* release semaphore */
            sem.Release();
        }

        /* unprotect flash before writing */
        private async Task WriteUnprotect()
        {
            /* command word */
            var tx = new byte[2];
            /* temporary storage for response bytes */
            var tmp = new byte[1];

            /* command code */
            tx[0] = (byte)STCmds.WR_UNPROTECT;
            /* checksum */
            tx[1] = ComputeChecksum(tx, 0, 1);

            /* try to send command and wait for response */
            await sem.WaitAsync().ConfigureAwait(false);
            try {
                /* send bytes */
                await SerialWrite(tx, 0, 2).ConfigureAwait(false);
                /* wait for response code */
                await SerialRead(tmp, 0, 1).ConfigureAwait(false);
                /* check response code */
                if (tmp[0] != (byte)STResps.ACK)
                    throw new STBootException("Command Rejected");

                /* wait for response code. use longer timeout, erase might
                 * take a while or two. */
                await SerialRead(tmp, 0, 1).ConfigureAwait(false);
                /* check response code */
                if (tmp[0] != (byte)STResps.ACK)
                    throw new STBootException("Write Unprotect Rejected");

                /* oops, something baaad happened! */
            } finally {
                /* release semaphore */
                sem.Release();
            }
        }

        /* unprotect flash before reading */
        private async Task ReadUnprotect()
        {
            /* command word */
            var tx = new byte[2];
            /* temporary storage for response bytes */
            var tmp = new byte[1];

            /* command code */
            tx[0] = (byte)STCmds.RD_UNPROTECT;
            /* checksum */
            tx[1] = ComputeChecksum(tx, 0, 1);

            /* try to send command and wait for response */
            await sem.WaitAsync().ConfigureAwait(false);
            try {
                /* send bytes */
                await SerialWrite(tx, 0, 2).ConfigureAwait(false);
                /* wait for response code */
                await SerialRead(tmp, 0, 1).ConfigureAwait(false);
                /* check response code */
                if (tmp[0] != (byte)STResps.ACK)
                    throw new STBootException("Command Rejected");

                /* wait for response code. use longer timeout, erase might
                 * take a while or two. */
                await SerialRead(tmp, 0, 1, 30000).ConfigureAwait(false);
                /* check response code */
                if (tmp[0] != (byte)STResps.ACK)
                    throw new STBootException("Write Unprotect Rejected");

                /* oops, something baaad happened! */
            } finally {
                /* release semaphore */
                sem.Release();
            }
        }

        /* compute checksum */
        /// <summary>Computes the protocol checksum by XORing the selected bytes with an initial value of 0xFF.</summary>
        private byte ComputeChecksum(byte[] data, int offset, int count)
        {
            /* initial value */
            byte xor = 0xff;
            /* compute */
            for (int i = offset; i < count + offset; i++)
                xor ^= data[i];

            /* return value */
            return xor;
        }

        private Stream testTransport;
        /// <summary>Uses a caller-provided transport for protocol validation without opening a physical serial port.</summary>
        internal STBoot(Stream transport) : this() { testTransport = transport; }
        private Task SerialWrite(byte[] data, int offset, int count)
        {
            return Task.Run(() => {
                if (testTransport != null) testTransport.Write(data, offset, count);
                else sp.Write(data, offset, count);
            });
        }
        private Task SerialRead(byte[] data, int offset, int count) => SerialRead(data, offset, count, 1000);
        /// <summary>Accumulates a complete response on a worker, rejecting EOF and enforcing the millisecond read deadline.</summary>
        private Task SerialRead(byte[] data, int offset, int count, int timeout)
        {
            return Task.Run(() => {
                int read = 0;
                if (sp != null) sp.ReadTimeout = timeout;
                var deadline = System.Diagnostics.Stopwatch.StartNew();
                while (read < count) {
                    if (deadline.ElapsedMilliseconds >= timeout) throw new TimeoutException("Bootloader read timed out.");
                    int n = testTransport != null ? testTransport.Read(data, offset + read, count - read) : sp.Read(data, offset + read, count - read);
                    if (n == 0) throw new EndOfStreamException("Incomplete bootloader response.");
                    read += n;
                }
            });
        }
    }
}

