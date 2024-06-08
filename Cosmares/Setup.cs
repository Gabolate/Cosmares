using Cosmos.Core.Memory;
using Cosmos.HAL.BlockDevice;
using Cosmos.System.FileSystem;
using System;
using Cosmos.System_Plugs.System;
using System.Collections.Generic;
using System.Linq;
using System.IO.Hashing;
using System.Text;
using System.Threading.Tasks;
using static System.Reflection.Metadata.BlobBuilder;
using static Cosmos.HAL.BlockDevice.GPT;
using System.Collections.Concurrent;
using Cosmos.System.FileSystem.ISO9660;
using Cosmos.System.FileSystem.FAT;
using System.Collections;

namespace Cosmares
{
    //Cosmares Remake 1.1.1 by Gabolate. Inspired by the original Cosmares made by PratyushKing

    public class Setup
    {
        /// <summary>
        /// Installs a Cosmos Kernel into the HDD
        /// </summary>
        /// <param name="system">
        /// The system's ISO bytes
        /// </param>
        /// <param name="disk">
        /// The disk to install the system (must be GPT)
        /// </param>
        /// <param name="partition">
        /// The partition index to use
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the 'system' byte array is null or empty
        /// </exception>
        /// <exception cref="FormatException">
        /// Thrown when the specified disk is not GPT
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the specified partition index is out of range
        /// </exception>
        /// <exception cref="Exception">
        /// Thrown when there is not enough space in the partition
        /// </exception>
        /// <exception cref="AccessViolationException">
        /// Thrown when a system is currently being setup by PartialInstall
        /// </exception>
        public static void Install(ReadOnlySpan<byte> system, Disk disk, uint partition, bool useHeapCollect = true, bool useExtraInfo = false)
        {
            //Make some checks before installing:
            if (!PartialInstallAvailable) //Check if there are no other systems being installed
            {
                throw new AccessViolationException("There is another system being installed");
            }
            if (system == null || system.Length < 1) //Check if the system bytes exist
            {
                throw new ArgumentNullException("system");
            }
            if (!GPT.IsGPTPartition(disk.Host)) //Check if the disk is GPT
            {
                throw new FormatException("The disk is not GPT!");
            }
            GPT gpt = new GPT(disk.Host); //Create a 'GPT' variable to get the partitions
            if (partition > gpt.Partitions.Count - 1) //Check if the partition exists
            {
                throw new ArgumentOutOfRangeException("partition");
            }
            if (gpt.Partitions[(int)partition].SectorCount * 512 < (ulong)system.Length + (128 * 512)) //Check if the partition has enough space for the system (plus 64KB just in case)
            {
                throw new Exception("Not enough space in the partition");
            }
            //Installing process:
            byte[] block = new byte[128 * 512]; //Create a byte array to store 64KB chunks
            ulong index = 0; //Create a ulong to store the current index in the 'block' byte array
            ulong currentBlock = 0; //Create a ulong to keep track of the current data block
            for (ulong i = 0; i < (ulong)system.Length; i++, index++)
            {
                if (index == 128 * 512) //Code to execute when the 'block' byte array is full
                {
                    index = 0; //Reset the byte index
                    disk.Host.WriteBlock(currentBlock + gpt.Partitions[(int)partition].StartSector, 128uL, ref block); //Write the data block in the partition
                    Array.Clear(block); //Clear the 'block' byte array
                    currentBlock += 128; //Skip 128 blocks
                    if (useHeapCollect)
                    {
                        Heap.Collect();
                    }
                }
                block[index] = system[(int)i]; //Store the current system byte in the 'block' byte array
            }
            if (useExtraInfo && useExtra) //Add extra system info to the partition
            {
                byte[] firstBlock = new byte[512];
                disk.Host.ReadBlock(gpt.Partitions[(int)partition].StartSector, 1uL, ref firstBlock);
                Array.Copy(extraBytes, firstBlock, 300);
                firstBlock[8] = 0;
                disk.Host.WriteBlock(gpt.Partitions[(int)partition].StartSector, 1uL, ref firstBlock);
            }
        }



        /// <summary>
        /// Installs a Cosmos Kernel into the HDD
        /// </summary>
        /// <param name="system">
        /// The system's ISO bytes
        /// </param>
        /// <param name="disk">
        /// The disk to install the system (must be GPT)
        /// </param>
        /// <param name="partition">
        /// The partition index to use
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the 'system' byte array is null or empty
        /// </exception>
        /// <exception cref="FormatException">
        /// Thrown when the specified disk is not GPT
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the specified partition index is out of range
        /// </exception>
        /// <exception cref="Exception">
        /// Thrown when there is not enough space in the partition
        /// </exception>
        /// <exception cref="AccessViolationException">
        /// Thrown when a system is currently being setup by PartialInstall
        /// </exception>
        public static void Install(ReadOnlySpan<byte> system, BlockDevice disk, uint partition, bool useHeapCollect = true, bool useExtraInfo = false) //Same as the Install() function, this one was made for compatibility
        {
            //Make some checks before installing:
            if (!PartialInstallAvailable) //Check if there are no other systems being installed
            {
                throw new AccessViolationException("There is another system being installed");
            }
            if (system == null || system.Length < 1) //Check if the system bytes exist
            {
                throw new ArgumentNullException("system");
            }
            if (!GPT.IsGPTPartition(disk)) //Check if the disk is GPT
            {
                throw new FormatException("The disk is not GPT!");
            }
            GPT gpt = new GPT(disk); //Create a 'GPT' variable to get the partitions
            if (partition > gpt.Partitions.Count - 1) //Check if the partition exists
            {
                throw new ArgumentOutOfRangeException("partition");
            }
            if (gpt.Partitions[(int)partition].SectorCount * 512 < (ulong)system.Length + (128 * 512)) //Check if the partition has enough space for the system (plus 64KB just in case)
            {
                throw new Exception("Not enough space in the partition");
            }
            //Installing process:
            byte[] block = new byte[128 * 512]; //Create a byte array to store 64KB chunks
            ulong index = 0; //Create a ulong to store the current index in the 'block' byte array
            ulong currentBlock = 0; //Create a ulong to keep track of the current data block
            for (ulong i = 0; i < (ulong)system.Length; i++, index++)
            {
                if (index == 128 * 512) //Code to execute when the 'block' byte array is full
                {
                    index = 0; //Reset the byte index
                    disk.WriteBlock(currentBlock + gpt.Partitions[(int)partition].StartSector, 128uL, ref block); //Write the data block in the partition
                    Array.Clear(block); //Clear the 'block' byte array
                    currentBlock += 128; //Skip 128 blocks
                    if (useHeapCollect)
                    {
                        Heap.Collect();
                    }
                }
                block[index] = system[(int)i]; //Store the current system byte in the 'block' byte array
            }
            if (useExtraInfo && useExtra) //Add extra system info to the partition
            {
                byte[] firstBlock = new byte[512];
                disk.ReadBlock(gpt.Partitions[(int)partition].StartSector, 1uL, ref firstBlock);
                Array.Copy(extraBytes, firstBlock, 300);
                firstBlock[8] = 1;
                disk.WriteBlock(gpt.Partitions[(int)partition].StartSector, 1uL, ref firstBlock);
            }
        }



        /// <summary>
        /// The size of the system byte array
        /// </summary>
        private static uint SystemCRC32 = 0;
        /// <summary>
        /// The next block to store
        /// </summary>
        private static ulong CurrentBlock = 0uL;
        /// <summary>
        /// The index in the system byte array
        /// </summary>
        private static int Index = 0;
        /// <summary>
        /// Tells if the system is already installed
        /// </summary>
        private static bool Done = false;
        /// <summary>
        /// Tells if its ready to install a new system
        /// </summary>
        private static bool PartialInstallAvailable = true;

        /// <summary>
        /// Installs a Cosmos Kernel into the HDD while allowing to make pauses between block writes
        /// </summary>
        /// <param name="system">
        /// The system's ISO bytes
        /// </param>
        /// <param name="disk">
        /// The disk to install the system (must be GPT)
        /// </param>
        /// <param name="partition">
        /// The partition index to use
        /// </param>
        /// <returns>
        /// A decimal with the percentage of the installation process (returns -1 if the installation is finished)
        /// </returns>
        /// <exception cref="DataMisalignedException">
        /// Thrown when the 'system' byte array changed its content during a block write pause
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the 'system' byte array is null or empty
        /// </exception>
        /// <exception cref="FormatException">
        /// Thrown when the specified disk is not GPT
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the specified partition index is out of range
        /// </exception>
        /// <exception cref="Exception">
        /// Thrown when there is not enough space in the partition
        /// </exception>
        public static decimal PartialInstall(ReadOnlySpan<byte> system, Disk disk, uint partition, bool useHeapCollect = false, bool useExtraInfo = false)
        {
            if (Done && Crc32.HashToUInt32(system) == SystemCRC32) //Check if it is already completed
            {
                return -1;
            }
            else if (Done && Crc32.HashToUInt32(system) != SystemCRC32) //Reset the variables in case its already done and another OS will be installed
            {
                Done = false;
                PartialInstallAvailable = true;
            }
            else if (!Done && Crc32.HashToUInt32(system) != SystemCRC32 && !PartialInstallAvailable) //Check if its not done yet but the 'system' byte array was changed
            {
                throw new DataMisalignedException("The specified system bytes are not the array used originally");
            }
            GPT gpt = new GPT(disk.Host); //Create a 'GPT' variable to access the partitions
            if (PartialInstallAvailable)
            {
                //Make some checks before installing:
                if (system == null || system.Length < 1) //Check if the system bytes exist
                {
                    throw new ArgumentNullException("system");
                }
                if (!GPT.IsGPTPartition(disk.Host)) //Check if the disk is GPT
                {
                    throw new FormatException("The disk is not GPT!");
                }
                if (partition > gpt.Partitions.Count - 1) //Check if the partition exists
                {
                    throw new ArgumentOutOfRangeException("partition");
                }
                if (gpt.Partitions[(int)partition].SectorCount * 512 < (ulong)system.Length + (128 * 512)) //Check if the partition has enough space for the system (plus 64KB just in case)
                {
                    throw new Exception("Not enough space in the partition");
                }
                PartialInstallAvailable = false;
                SystemCRC32 = Crc32.HashToUInt32(system);
                CurrentBlock = 0uL;
                Index = 0;
            }

            //Installing process:
            byte[] block = new byte[128 * 512]; //Create a byte array to store 64KB chunks
            ulong index = 0; //Create a ulong to store the current index in the 'block' byte array
            ulong currentBlock = CurrentBlock; //Create a ulong to keep track of the current data block
            for (ulong i = (ulong)Index; i < (ulong)system.Length; i++, index++, Index++)
            {
                if (index == 128 * 512) //Code to execute when the 'block' byte array is full
                {
                    index = 0; //Reset the byte index
                    disk.Host.WriteBlock(currentBlock + gpt.Partitions[(int)partition].StartSector, 128uL, ref block); //Write the data block in the partition
                    Array.Clear(block); //Clear the 'block' byte array
                    CurrentBlock += 128; //Skip 128 blocks
                    if (useHeapCollect)
                    {
                        Heap.Collect();
                    }
                    break;
                }
                block[index] = system[(int)i]; //Store the current system byte in the 'block' byte array
            }
            if ((decimal)Index / system.Length * 100 == 100) //Finish the installation if it is done
            {
                Done = true;
            }
            if (Done) //Returns the percentage of the installation process and -1 in case its finished
            {
                if (useExtraInfo && useExtra) //Add extra system info to the partition
                {
                    byte[] firstBlock = new byte[512];
                    disk.Host.ReadBlock(gpt.Partitions[(int)partition].StartSector, 1uL, ref firstBlock);
                    Array.Copy(extraBytes, firstBlock, 300);
                    firstBlock[8] = 2;
                    disk.Host.WriteBlock(gpt.Partitions[(int)partition].StartSector, 1uL, ref firstBlock);
                }
                return -1;
            }
            else
            {
                return (decimal)Index / system.Length * 100;
            }
        }




        /// <summary>
        /// Installs a Cosmos Kernel into the HDD while allowing to make pauses between block writes
        /// </summary>
        /// <param name="system">
        /// The system's ISO bytes
        /// </param>
        /// <param name="disk">
        /// The disk to install the system (must be GPT)
        /// </param>
        /// <param name="partition">
        /// The partition index to use
        /// </param>
        /// <returns>
        /// A decimal with the percentage of the installation process (returns -1 if the installation is finished)
        /// </returns>
        /// <exception cref="DataMisalignedException">
        /// Thrown when the 'system' byte array changed its content during a block write pause
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the 'system' byte array is null or empty
        /// </exception>
        /// <exception cref="FormatException">
        /// Thrown when the specified disk is not GPT
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the specified partition index is out of range
        /// </exception>
        /// <exception cref="Exception">
        /// Thrown when there is not enough space in the partition
        /// </exception>
        public static decimal PartialInstall(ReadOnlySpan<byte> system, BlockDevice disk, uint partition, bool useHeapCollect = false, bool useExtraInfo = false) //Same as the PartialInstall(), this was made for compatiblity
        {
            if (Done && Crc32.HashToUInt32(system) == SystemCRC32) //Check if it is already completed
            {
                return -1;
            }
            else if (Done && Crc32.HashToUInt32(system) != SystemCRC32) //Reset the variables in case its already done and another OS will be installed
            {
                Done = false;
                PartialInstallAvailable = true;
            }
            else if (!Done && Crc32.HashToUInt32(system) != SystemCRC32 && !PartialInstallAvailable) //Check if its not done yet but the 'system' byte array was changed
            {
                throw new DataMisalignedException("The specified system bytes are not the array used originally");
            }
            GPT gpt = new GPT(disk); //Create a 'GPT' variable to access the partitions
            if (PartialInstallAvailable)
            {
                //Make some checks before installing:
                if (system == null || system.Length < 1) //Check if the system bytes exist
                {
                    throw new ArgumentNullException("system");
                }
                if (!GPT.IsGPTPartition(disk)) //Check if the disk is GPT
                {
                    throw new FormatException("The disk is not GPT!");
                }
                if (partition > gpt.Partitions.Count - 1) //Check if the partition exists
                {
                    throw new ArgumentOutOfRangeException("partition");
                }
                if (gpt.Partitions[(int)partition].SectorCount * 512 < (ulong)system.Length + (128 * 512)) //Check if the partition has enough space for the system (plus 64KB just in case)
                {
                    throw new Exception("Not enough space in the partition");
                }
                PartialInstallAvailable = false;
                SystemCRC32 = Crc32.HashToUInt32(system);
                CurrentBlock = 0uL;
                Index = 0;
            }

            //Installing process:
            byte[] block = new byte[128 * 512]; //Create a byte array to store 64KB chunks
            ulong index = 0; //Create a ulong to store the current index in the 'block' byte array
            ulong currentBlock = CurrentBlock; //Create a ulong to keep track of the current data block
            for (ulong i = (ulong)Index; i < (ulong)system.Length; i++, index++, Index++)
            {
                if (index == 128 * 512) //Code to execute when the 'block' byte array is full
                {
                    index = 0; //Reset the byte index
                    disk.WriteBlock(currentBlock + gpt.Partitions[(int)partition].StartSector, 128uL, ref block); //Write the data block in the partition
                    Array.Clear(block); //Clear the 'block' byte array
                    CurrentBlock += 128; //Skip 128 blocks
                    if (useHeapCollect)
                    {
                        Heap.Collect();
                    }
                    break;
                }
                block[index] = system[(int)i]; //Store the current system byte in the 'block' byte array
            }
            if ((decimal)Index / system.Length * 100 == 100) //Finish the installation if it is done
            {
                Done = true;
            }
            if (Done) //Returns the percentage of the installation process and -1 in case its finished
            {
                if (useExtraInfo && useExtra) //Add extra system info to the partition
                {
                    byte[] firstBlock = new byte[512];
                    disk.ReadBlock(gpt.Partitions[(int)partition].StartSector, 1uL, ref firstBlock);
                    Array.Copy(extraBytes, firstBlock, 300);
                    firstBlock[8] = 3;
                    disk.WriteBlock(gpt.Partitions[(int)partition].StartSector, 1uL, ref firstBlock);
                }
                return -1;
            }
            else
            {
                return (decimal)Index / system.Length * 100;
            }
        }

        /// <summary>
        /// Non-FAT FS list
        /// </summary>
        public enum FS
        {
            /// <summary>
            /// Unknown FS, possibly FAT
            /// </summary>
            Unknown,
            /// <summary>
            /// Windows NTFS
            /// </summary>
            NTFS,
            /// <summary>
            /// Includes EXT2, EXT3 and EXT4
            /// </summary>
            LinuxEXT,
            /// <summary>
            /// ExFAT fs
            /// </summary>
            EXFAT,
            /// <summary>
            /// FAT32 filesystem
            /// </summary>
            FAT32,
            /// <summary>
            /// Used for both FAT12 and FAT16
            /// </summary>
            FAT,
            /// <summary>
            /// FS used in CD-ROMs
            /// </summary>
            ISO9660,
            /// <summary>
            /// Gabolate's PlutonFS
            /// </summary>
            PlutonFS
        }

        /// <summary>
        /// Gets a non-FAT FS from a partition (if available)
        /// </summary>
        /// <param name="disk">
        /// The disk to use
        /// </param>
        /// <param name="partition">
        /// The partition to check
        /// </param>
        /// <returns>
        /// FS enum with the result
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the specified partition is out of range
        /// </exception>
        /// <exception cref="FormatException">
        /// Thrown when the specified disk is not GPT
        /// </exception>
        public static FS GetFS(Disk disk, uint partition)
        {
            if (!IsGPTPartition(disk.Host)) //Check if the disk is GPT
            {
                throw new FormatException("The disk is not GPT!");
            }
            GPT gpt = new GPT(disk.Host); //Create a 'GPT' variable to get the partitions' info
            if (partition >= gpt.Partitions.Count) //Check if the specified partition is out of range
            {
                throw new ArgumentOutOfRangeException("partition");
            }
            byte[] start = new byte[512 * 3];//Byte array for the first 1.5kb of the partition
            //Read the first 3 blocks of the partition
            disk.Host.ReadBlock(gpt.Partitions[(int)partition].StartSector, 3uL, ref start);
            if (Encoding.ASCII.GetString(start, 0, 8) == "PlutonFS") return FS.PlutonFS;
            if (Encoding.ASCII.GetString(start, 3, 8) == "EXFAT   ") return FS.EXFAT;
            if (start[1080] == 0x53 && start[1081] == 0xEF) return FS.LinuxEXT;
            if (Encoding.ASCII.GetString(start, 3, 4) == "NTFS") return FS.NTFS;
            if (start[38] == 40 || start[38] == 41) return FS.FAT;
            if (start[66] == 40 || start[66] == 41) return FS.FAT32;
            //Read block 64 to see if ISO6900 is present
            disk.Host.ReadBlock(gpt.Partitions[(int)partition].StartSector + 64uL, 3uL, ref start);
            if (Encoding.ASCII.GetString(start, 1, 5) == "CD001") return FS.ISO9660;
            return FS.Unknown; //Return 'Unknown' if all the previous checks failed
        }


        /// <summary>
        /// Gets a non-FAT FS from a partition (if available)
        /// </summary>
        /// <param name="disk">
        /// The disk to use
        /// </param>
        /// <param name="partition">
        /// The partition to check
        /// </param>
        /// <returns>
        /// FS enum with the result
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the specified partition is out of range
        /// </exception>
        /// <exception cref="FormatException">
        /// Thrown when the specified disk is not GPT
        /// </exception>
        public static FS GetFS(BlockDevice disk, uint partition) //Same as GetFS(), this one was made for compatibility
        {
            if (!IsGPTPartition(disk)) //Check if the disk is GPT
            {
                throw new FormatException("The disk is not GPT!");
            }
            GPT gpt = new GPT(disk); //Create a 'GPT' variable to get the partitions' info
            if (partition >= gpt.Partitions.Count) //Check if the specified partition is out of range
            {
                throw new ArgumentOutOfRangeException("partition");
            }
            byte[] start = new byte[512 * 3];//Byte array for the first 1.5kb of the partition
            //Read the first 3 blocks of the partition
            disk.ReadBlock(gpt.Partitions[(int)partition].StartSector, 3uL, ref start);
            if (Encoding.ASCII.GetString(start, 0, 8) == "PlutonFS") return FS.PlutonFS;
            if (Encoding.ASCII.GetString(start, 3, 8) == "EXFAT   ") return FS.EXFAT;
            if (start[1080] == 0x53 && start[1081] == 0xEF) return FS.LinuxEXT;
            if (Encoding.ASCII.GetString(start, 3, 4) == "NTFS") return FS.NTFS;
            if (start[38] == 40 || start[38] == 41) return FS.FAT;
            if (start[66] == 40 || start[66] == 41) return FS.FAT32;
            //Read block 64 to see if ISO6900 is present
            disk.ReadBlock(gpt.Partitions[(int)partition].StartSector + 64uL, 3uL, ref start);
            if (Encoding.ASCII.GetString(start, 1, 5) == "CD001") return FS.ISO9660;
            return FS.Unknown; //Return 'Unknown' if all the previous checks failed
        }

        /// <summary>
        /// Deletes a GPT partition
        /// </summary>
        /// <param name="disk">
        /// The disk to use
        /// </param>
        /// <param name="partition">
        /// The partition to remove
        /// </param>
        /// <exception cref="FormatException">
        /// Thrown when the disk is not GPT
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the specified partition is out of range
        /// </exception>
        /// <exception cref="Exception">
        /// Thrown when the partition entry is empty
        /// </exception>
        public static void DeleteGPTpartition(Disk disk, uint partitionEntry)
        {
            if (!GPT.IsGPTPartition(disk.Host)) //Check if the disk is GPT
            {
                throw new FormatException("The disk is not GPT!");
            }
            GPT gpt = new GPT(disk.Host); //'GPT' variable used to get the partitions' information
            if (partitionEntry >= 128) //Check if the specified partition is out of range
            {
                throw new ArgumentOutOfRangeException("partition");
            }
            byte[] entries = new byte[512 * 33]; //Byte array to store the GPT header and the partition entries
            byte[] blankEntry = new byte[32]; //Byte array for blank entries
            bool isEmpty = false; //Tells if the entry is empty
            disk.Host.ReadBlock(1uL, 33uL, ref entries); //Read the GPT header and the partition entries
            using (MemoryStream stream = new MemoryStream(entries))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        for (int i = 0; i < 128; i++) //Loop trough all the entries until it finds the specified one
                        {
                            stream.Position = (i * 128) + 512; //Move the stream position to the current entry
                            if (i == partitionEntry) //Code to execute once it reaches the specified entry:
                            {
                                if (reader.ReadBytes(32).SequenceEqual(blankEntry)) //Check if the entry is empty
                                {
                                    isEmpty = true;
                                }
                                else
                                {
                                    stream.Position -= 32; //Go back to the start of the entry
                                    writer.Write(blankEntry); //Write blank GUIDs to the entry
                                    stream.Position = 16; //Go to the header CRC32 offset
                                    writer.Write(0); //Write a placeholder for the header CRC32 checksum
                                    stream.Position = 512; //Go to the start of the partition entries array
                                    //Generate a CRC32 checksum for the partition entries
                                    uint entriesCRC32 = Crc32.HashToUInt32(reader.ReadBytes(128 * 128));
                                    stream.Position = 88; //Go to the partition entries CRC32 offset
                                    writer.Write(entriesCRC32); //Write the partition entries CRC32 checksum
                                    stream.Position = 0; //Go to the start of the GPT header
                                    //Generate the GPT header CRC32
                                    uint headerCRC32 = Crc32.HashToUInt32(reader.ReadBytes(92));
                                    stream.Position = 16; //Go to the header CRC32 offset
                                    writer.Write(headerCRC32); //Write the header CRC32 checksum
                                    disk.Host.WriteBlock(1uL, 33uL, ref entries); //Write the GPT header and partition entries
                                }
                            }
                        }
                    }
                }
            }
            if (isEmpty)
            {
                throw new Exception("The partition entry is empty!");
            }
        }


        /// <summary>
        /// Deletes a GPT partition
        /// </summary>
        /// <param name="disk">
        /// The disk to use
        /// </param>
        /// <param name="partition">
        /// The partition to remove
        /// </param>
        /// <exception cref="FormatException">
        /// Thrown when the disk is not GPT
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the specified partition is out of range
        /// </exception>
        /// <exception cref="Exception">
        /// Thrown when the partition entry is empty
        /// </exception>
        public static void DeleteGPTpartition(BlockDevice disk, uint partitionEntry) //Same as DeleteGPTpartition(), this one was made for compatiblity
        {
            if (!GPT.IsGPTPartition(disk)) //Check if the disk is GPT
            {
                throw new FormatException("The disk is not GPT!");
            }
            GPT gpt = new GPT(disk); //'GPT' variable used to get the partitions' information
            if (partitionEntry >= 128) //Check if the specified partition is out of range
            {
                throw new ArgumentOutOfRangeException("partition");
            }
            byte[] entries = new byte[512 * 33]; //Byte array to store the GPT header and the partition entries
            byte[] blankEntry = new byte[32]; //Byte array for blank entries
            bool isEmpty = false; //Tells if the entry is empty
            disk.ReadBlock(1uL, 33uL, ref entries); //Read the GPT header and the partition entries
            using (MemoryStream stream = new MemoryStream(entries))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        for (int i = 0; i < 128; i++) //Loop trough all the entries until it finds the specified one
                        {
                            stream.Position = (i * 128) + 512; //Move the stream position to the current entry
                            if (i == partitionEntry) //Code to execute once it reaches the specified entry:
                            {
                                if (reader.ReadBytes(32).SequenceEqual(blankEntry)) //Check if the entry is empty
                                {
                                    isEmpty = true;
                                }
                                else
                                {
                                    stream.Position -= 32; //Go back to the start of the entry
                                    writer.Write(blankEntry); //Write blank GUIDs to the entry
                                    stream.Position = 16; //Go to the header CRC32 offset
                                    writer.Write(0); //Write a placeholder for the header CRC32 checksum
                                    stream.Position = 512; //Go to the start of the partition entries array
                                    //Generate a CRC32 checksum for the partition entries
                                    uint entriesCRC32 = Crc32.HashToUInt32(reader.ReadBytes(128 * 128));
                                    stream.Position = 88; //Go to the partition entries CRC32 offset
                                    writer.Write(entriesCRC32); //Write the partition entries CRC32 checksum
                                    stream.Position = 0; //Go to the start of the GPT header
                                    //Generate the GPT header CRC32
                                    uint headerCRC32 = Crc32.HashToUInt32(reader.ReadBytes(92));
                                    stream.Position = 16; //Go to the header CRC32 offset
                                    writer.Write(headerCRC32); //Write the header CRC32 checksum
                                    disk.WriteBlock(1uL, 33uL, ref entries); //Write the GPT header and partition entries
                                }
                            }
                        }
                    }
                }
            }
            if (isEmpty)
            {
                throw new Exception("The partition entry is empty!");
            }
        }

        /// <summary>
        /// Creates a GPT partition
        /// </summary>
        /// <param name="disk">
        /// The disk to use
        /// </param>
        /// <param name="startingBlock">
        /// The first block of the partition (cannot be less than 34)
        /// </param>
        /// <param name="sizeInMB">
        /// The size in megabytes of the partition
        /// </param>
        /// <returns>
        /// The entry where the partition was made (if it returns 128, a critical error happened)
        /// </returns>
        /// <exception cref="FormatException">
        /// Thrown when the disk is not GPT
        /// </exception>
        /// <exception cref="EndOfStreamException">
        /// Thrown when all the  partition entries are full
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the starting block is less than 34 or higher than the total blocks in the disk
        /// </exception>
        /// <exception cref="Exception">
        /// Thrown when the partition overlaps an already existing one
        /// </exception>
        public static uint CreateGPTpartition(Disk disk, ulong startingBlock, ulong sizeInMB)
        {
            if (!GPT.IsGPTPartition(disk.Host)) //Check if the disk is GPT
            {
                throw new FormatException("The disk is not GPT!");
            }
            GPT gpt = new GPT(disk.Host); //'GPT' variable to access the partitions' information
            if (gpt.Partitions.Count == 128) //Check if all the partition entries are reserved
            {
                throw new EndOfStreamException("The partition entries are full!");
            }
            if (startingBlock < 34 || startingBlock > disk.Host.BlockCount) //Check if the starting block is between the GPT partition entries and the end of the disk
            {
                throw new ArgumentOutOfRangeException("startingBlock");
            }
            ulong sizeInBlocks = (sizeInMB * 1024 * 1024) / 512; //The size in blocks of the partition
            byte[] entries = new byte[512 * 33]; //Byte array for the GPT partition entries
            byte[] blankEntry = new byte[32]; //Byte array for blank partition entries
            ulong tmp1 = 0; //Temporal ulong used for the partition overlapping check
            ulong tmp2 = 0; //Temporal ulong used for the partition overlapping check
            bool shouldExit = false; //Tells if the function should exit
            //Partition type GUID (Linux fs type)
            byte[] typeGUID = { 175, 61, 198, 15, 131, 132, 114, 71, 142, 121, 61, 105, 216, 71, 125, 228 };
            disk.Host.ReadBlock(1uL, 33uL, ref entries); //Read the GPT partition entries
            using (MemoryStream stream = new MemoryStream(entries))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        for (int i = 0; i < 128; i++) //Partition overlapping check
                        {
                            stream.Position = (i * 128) + 512; //Move the stream position to the current entry
                            if (!reader.ReadBytes(32).SequenceEqual(blankEntry)) //Skip the entry if it is empty
                            {
                                tmp1 = reader.ReadUInt64(); //Store the starting block of the entry
                                tmp2 = reader.ReadUInt64(); //Store the last block of the entry
                                if (tmp1 != 0 && tmp2 != 0) //Skip the entry if the starting and last block are 0
                                {
                                    //Check if the specified partition and the current entry overlap
                                    if (tmp1 <= sizeInBlocks + startingBlock && startingBlock <= tmp2)
                                    {
                                        shouldExit = true;
                                    }
                                }
                            }
                        }

                        if (!shouldExit)
                        {
                            for (int i = 0; i < 128; i++) //Loop trough all the partition entries looking for an empty one
                            {
                                stream.Position = (i * 128) + 512; //Set the stream position to be at the current entry
                                if (reader.ReadBytes(32).SequenceEqual(blankEntry)) //Check if the entry is available
                                {
                                    stream.Position = (i * 128) + 512; //Return to the start of the entry
                                    writer.Write(typeGUID); //Write the partition type GUID
                                    writer.Write(GuidImpl.NewGuid().ToByteArray()); //Write a random GUID
                                    writer.Write(startingBlock); //Write the starting block
                                    writer.Write(startingBlock + sizeInBlocks); //Write the last block of the partition
                                    writer.Write(new byte[80]); //Write empty bytes in case another partition used this entry before and included more data
                                    stream.Position = 16; //Go to the header CRC32 offset
                                    writer.Write(0); //Write the placeholder bytes for the header CRC32 checksum
                                    stream.Position = 512; //Go to the start of the partition entries array
                                    //Generate the CRC32 checksum for the partition entries
                                    uint entriesCRC32 = Crc32.HashToUInt32(reader.ReadBytes(128 * 128));
                                    stream.Position = 88; //Go to the partition entries CRC32 offset
                                    writer.Write(entriesCRC32); //Write the partition entries CRC32 checksum
                                    stream.Position = 0; //Go to the start of the GPT header
                                    //Generate the CRC32 checksum for the GPT header
                                    uint headerCRC32 = Crc32.HashToUInt32(reader.ReadBytes(92));
                                    stream.Position = 16; //Go to the header CRC32 offset
                                    writer.Write(headerCRC32); //Write the header CRC32 checksum
                                    disk.Host.WriteBlock(1uL, 33uL, ref entries); //Write the GPT header and partition entries
                                    return (uint)i; //Return the used partition entry
                                }
                            }
                            return 128; //Return 128 (shouldn't get to this stage, if for some reason it happens, the partition entries array is corrupted)
                        }
                    }
                }
            }
            throw new Exception("The partition is overlapping an already existing one");
        }


        /// <summary>
        /// Creates a GPT partition
        /// </summary>
        /// <param name="disk">
        /// The disk to use
        /// </param>
        /// <param name="startingBlock">
        /// The first block of the partition (cannot be less than 34)
        /// </param>
        /// <param name="sizeInMB">
        /// The size in megabytes of the partition
        /// </param>
        /// <returns>
        /// The entry where the partition was made (if it returns 128, a critical error happened)
        /// </returns>
        /// <exception cref="FormatException">
        /// Thrown when the disk is not GPT
        /// </exception>
        /// <exception cref="EndOfStreamException">
        /// Thrown when all the  partition entries are full
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the starting block is less than 34 or higher than the total blocks in the disk
        /// </exception>
        /// <exception cref="Exception">
        /// Thrown when the partition overlaps an already existing one
        /// </exception>
        public static uint CreateGPTpartition(BlockDevice disk, ulong startingBlock, ulong sizeInMB) //Same as CreateGPTpartition(), this one was made for compatiblity
        {
            if (!GPT.IsGPTPartition(disk)) //Check if the disk is GPT
            {
                throw new FormatException("The disk is not GPT!");
            }
            GPT gpt = new GPT(disk); //'GPT' variable to access the partitions' information
            if (gpt.Partitions.Count == 128) //Check if all the partition entries are reserved
            {
                throw new EndOfStreamException("The partition entries are full!");
            }
            if (startingBlock < 34 || startingBlock > disk.BlockCount) //Check if the starting block is between the GPT partition entries and the end of the disk
            {
                throw new ArgumentOutOfRangeException("startingBlock");
            }
            ulong sizeInBlocks = (sizeInMB * 1024 * 1024) / 512; //The size in blocks of the partition
            byte[] entries = new byte[512 * 33]; //Byte array for the GPT partition entries
            byte[] blankEntry = new byte[32]; //Byte array for blank partition entries
            ulong tmp1 = 0; //Temporal ulong used for the partition overlapping check
            ulong tmp2 = 0; //Temporal ulong used for the partition overlapping check
            bool shouldExit = false; //Tells if the function should exit
            //Partition type GUID (Linux fs type)
            byte[] typeGUID = { 175, 61, 198, 15, 131, 132, 114, 71, 142, 121, 61, 105, 216, 71, 125, 228 };
            disk.ReadBlock(1uL, 33uL, ref entries); //Read the GPT partition entries
            using (MemoryStream stream = new MemoryStream(entries))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        for (int i = 0; i < 128; i++) //Partition overlapping check
                        {
                            stream.Position = (i * 128) + 512; //Move the stream position to the current entry
                            if (!reader.ReadBytes(32).SequenceEqual(blankEntry)) //Skip the entry if it is empty
                            {
                                tmp1 = reader.ReadUInt64(); //Store the starting block of the entry
                                tmp2 = reader.ReadUInt64(); //Store the last block of the entry
                                if (tmp1 != 0 && tmp2 != 0) //Skip the entry if the starting and last block are 0
                                {
                                    //Check if the specified partition and the current entry overlap
                                    if (tmp1 <= sizeInBlocks + startingBlock && startingBlock <= tmp2)
                                    {
                                        shouldExit = true;
                                    }
                                }
                            }
                        }

                        if (!shouldExit)
                        {
                            for (int i = 0; i < 128; i++) //Loop trough all the partition entries looking for an empty one
                            {
                                stream.Position = (i * 128) + 512; //Set the stream position to be at the current entry
                                if (reader.ReadBytes(32).SequenceEqual(blankEntry)) //Check if the entry is available
                                {
                                    stream.Position = (i * 128) + 512; //Return to the start of the entry
                                    writer.Write(typeGUID); //Write the partition type GUID
                                    writer.Write(GuidImpl.NewGuid().ToByteArray()); //Write a random GUID
                                    writer.Write(startingBlock); //Write the starting block
                                    writer.Write(startingBlock + sizeInBlocks); //Write the last block of the partition
                                    writer.Write(new byte[80]); //Write empty bytes in case another partition used this entry before and included more data
                                    stream.Position = 16; //Go to the header CRC32 offset
                                    writer.Write(0); //Write the placeholder bytes for the header CRC32 checksum
                                    stream.Position = 512; //Go to the start of the partition entries array
                                    //Generate the CRC32 checksum for the partition entries
                                    uint entriesCRC32 = Crc32.HashToUInt32(reader.ReadBytes(128 * 128));
                                    stream.Position = 88; //Go to the partition entries CRC32 offset
                                    writer.Write(entriesCRC32); //Write the partition entries CRC32 checksum
                                    stream.Position = 0; //Go to the start of the GPT header
                                    //Generate the CRC32 checksum for the GPT header
                                    uint headerCRC32 = Crc32.HashToUInt32(reader.ReadBytes(92));
                                    stream.Position = 16; //Go to the header CRC32 offset
                                    writer.Write(headerCRC32); //Write the header CRC32 checksum
                                    disk.WriteBlock(1uL, 33uL, ref entries); //Write the GPT header and partition entries
                                    return (uint)i; //Return the used partition entry
                                }
                            }
                            return 128; //Return 128 (shouldn't get to this stage, if for some reason it happens, the partition entries array is corrupted)
                        }
                    }
                }
            }
            throw new Exception("The partition is overlapping an already existing one");
        }

        /// <summary>
        /// Stores 300 extra bytes for the system to install
        /// </summary>
        /// <param name="extraInfo">
        /// The bytes to store
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when there are not 300 bytes
        /// </exception>
        public static void StoreInformation(ReadOnlySpan<byte> extraInfo)
        {
            if (extraInfo.Length != 300)
            {
                throw new ArgumentOutOfRangeException("extraInfo");
            }
            extraInfo.CopyTo(extraBytes); //Copy the extra bytes
            useExtra = true;
        }


        private static bool useExtra = false;

        private static byte[] extraBytes = new byte[300];

        /// <summary>
        /// OSinfo, used for writing extra data when installing a system
        /// </summary>
        public struct OSinfo
        {
            /// <summary>
            /// The OS' name (136 chars max)
            /// </summary>
            public string osname;
            /// <summary>
            /// The creator of the OS (136 chars max)
            /// </summary>
            public string author;
            /// <summary>
            /// Major version (for example: '1'.2.3)
            /// </summary>
            public uint major;
            /// <summary>
            /// Minor version (for example: 1.'2'.3)
            /// </summary>
            public uint minor;
            /// <summary>
            /// Patch version (for example: 1.2.'3')
            /// </summary>
            public uint patch;
            /// <summary>
            /// Version year (for example: 1.2.3_'2024')
            /// </summary>
            public uint year;
            /// <summary>
            /// Version month (for example: 1.2.3_2024'06')
            /// </summary>
            public uint month;
            /// <summary>
            /// Version day (for example: 1.2.3_202406'07')
            /// </summary>
            public uint day;
            /// <summary>
            /// Development stage for the version
            /// </summary>
            public DevStage stage;
        }

        /// <summary>
        /// Available development stages (Alpha, Beta, Release Candidate, Release and Post Release Fixes)
        /// </summary>
        public enum DevStage
        {
            Alpha,
            Beta,
            ReleaseCandidate,
            Release,
            PostReleaseFixes
        }

        /// <summary>
        /// Stores extra information when installing an OS
        /// </summary>
        /// <param name="info">
        /// The information to use
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the OS' name or the author's name is more than 136 characters
        /// </exception>
        public static void StoreInformation(OSinfo info)
        {
            if (info.osname.Length > 136)
            {
                throw new ArgumentOutOfRangeException("info", "The OS' name is too long");
            }
            if (info.author.Length > 136)
            {
                throw new ArgumentOutOfRangeException("info", "The author's name is too long");
            }
            byte[] bytes = new byte[300]; //Create a byte array for the extra info
            using (MemoryStream stream = new MemoryStream(bytes))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    //Write that the OS has been installed with Cosmares
                    stream.Write(Encoding.ASCII.GetBytes("Cosmares"));
                    writer.Write((byte)0); //Write reserved byte
                    writer.Write(info.major); //Write major version
                    writer.Write(info.minor); //Write minor version
                    writer.Write(info.patch); //Write patch version
                    writer.Write(info.day); //Write version day
                    writer.Write(info.month); //Write version month
                    writer.Write(info.year); //Write version year
                    writer.Write((byte)info.stage); //Write the development stage
                    writer.Write(info.author); //Write the author's name
                    writer.Write(info.osname); //Write the OS' name
                    StoreInformation(bytes); //Write the OS information
                }
            }
        }

        /// <summary>
        /// If available, gets the information of an OS installed with Cosmares
        /// </summary>
        /// <param name="disk">
        /// The disk to use
        /// </param>
        /// <param name="partition">
        /// The partition to use
        /// </param>
        /// <returns>
        /// The OS' information
        /// </returns>
        /// <exception cref="FormatException">
        /// Thrown when the disk is not GPT
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// Thrown when the specified partition does not contain a Cosmares OS info
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the specified partition is out of range
        /// </exception>
        public static OSinfo GetInformation(Disk disk, uint partition)
        {
            if (!GPT.IsGPTPartition(disk.Host)) //Check if the disk is GPT
            {
                throw new FormatException("The disk is not GPT!");
            }
            GPT gpt = new GPT(disk.Host); //Create a 'GPT' variable to get the partitions information
            if (partition >= gpt.Partitions.Count) //Check if the partition is inside the range
            {
                throw new ArgumentOutOfRangeException("partition");
            }
            byte[] block = new byte[512];
            disk.Host.ReadBlock(gpt.Partitions[(int)partition].StartSector, 1uL, ref block);
            if (Encoding.ASCII.GetString(block, 0, 8) == "Cosmares")
            {
                using(MemoryStream stream = new MemoryStream(block))
                {
                    using(BinaryReader reader = new BinaryReader(stream))
                    {
                        stream.Position = 9;
                        return new OSinfo()
                        {
                            major = reader.ReadUInt32(),
                            minor = reader.ReadUInt32(),
                            patch = reader.ReadUInt32(),
                            day = reader.ReadUInt32(),
                            month = reader.ReadUInt32(),
                            year = reader.ReadUInt32(),
                            stage = (DevStage)reader.ReadByte(),
                            author = reader.ReadString(),
                            osname = reader.ReadString()
                        };
                    }
                }
            }
            else
            {
                throw new DriveNotFoundException("No OSes installed with Cosmares extra info found");
            }
        }

        /// <summary>
        /// If available, gets the information of an OS installed with Cosmares
        /// </summary>
        /// <param name="disk">
        /// The disk to use
        /// </param>
        /// <param name="partition">
        /// The partition to use
        /// </param>
        /// <returns>
        /// The OS' information
        /// </returns>
        /// <exception cref="FormatException">
        /// Thrown when the disk is not GPT
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// Thrown when the specified partition does not contain a Cosmares OS info
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the specified partition is out of range
        /// </exception>
        public static OSinfo GetInformation(BlockDevice disk, uint partition) //The same as GetInformation(), this one was made for compatiblity
        {
            if (!GPT.IsGPTPartition(disk)) //Check if the disk is GPT
            {
                throw new FormatException("The disk is not GPT!");
            }
            GPT gpt = new GPT(disk); //Create a 'GPT' variable to get the partitions information
            if (partition >= gpt.Partitions.Count) //Check if the partition is inside the range
            {
                throw new ArgumentOutOfRangeException("partition");
            }
            byte[] block = new byte[512];
            disk.ReadBlock(gpt.Partitions[(int)partition].StartSector, 1uL, ref block);
            if (Encoding.ASCII.GetString(block, 0, 8) == "Cosmares")
            {
                using (MemoryStream stream = new MemoryStream(block))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        stream.Position = 9;
                        return new OSinfo()
                        {
                            major = reader.ReadUInt32(),
                            minor = reader.ReadUInt32(),
                            patch = reader.ReadUInt32(),
                            day = reader.ReadUInt32(),
                            month = reader.ReadUInt32(),
                            year = reader.ReadUInt32(),
                            stage = (DevStage)reader.ReadByte(),
                            author = reader.ReadString(),
                            osname = reader.ReadString()
                        };
                    }
                }
            }
            else
            {
                throw new DriveNotFoundException("No OSes installed with Cosmares extra info found");
            }
        }
    }
}
