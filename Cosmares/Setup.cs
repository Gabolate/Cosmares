using Cosmos.Core.Memory;
using Cosmos.System.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Reflection.Metadata.BlobBuilder;

namespace Cosmares
{
    //Cosmares Remake 1.0 by Gabolate. Inspired by the original Cosmares made by PratyushKing

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
        public static void Install(byte[] system, Disk disk, uint partition)
        {
            //Make some checks before installing:
            if (system == null || system.Length < 1) //Check if the system bytes exist
            {
                throw new ArgumentNullException("system");
            }
            if (disk.IsMBR) //Check if the disk is GPT
            {
                throw new FormatException("The disk is not GPT!");
            }
            if (partition > disk.Partitions.Count - 1) //Check if the partition exists
            {
                throw new ArgumentOutOfRangeException("partition");
            }
            if (disk.Partitions[(int)partition].Host.BlockCount * disk.Partitions[(int)partition].Host.BlockSize < (ulong)system.Length + (64 * 512)) //Check if the partition has enough space for the system (plus 32KB just in case)
            {
                throw new Exception("Not enough space in the partition");
            }
            //Installing process:
            byte[] block = new byte[64 * 512]; //Create a byte array to store 32KB chunks
            ulong index = 0; //Create a ulong to store the current index in the 'block' byte array
            ulong currentBlock = 0; //Create a ulong to keep track of the current data block
            for (ulong i = 0; i < (ulong)system.Length; i++, index++)
            {
                if (index == 64 * 512) //Code to execute when the 'block' byte array is full
                {
                    index = 0; //Reset the byte index
                    disk.Partitions[(int)partition].Host.WriteBlock(currentBlock, 64uL, ref block); //Write the data block in the partition
                    Array.Clear(block); //Clear the 'block' byte array
                    currentBlock += 64; //Skip 64 blocks
                    Heap.Collect(); //Run GC Collection to prevent the kernel from crashing
                }
                block[index] = system[i]; //Store the current system byte in the 'block' byte array
            }
        }

        /// <summary>
        /// The size of the system byte array
        /// </summary>
        private static int SystemSize = 0;
        /// <summary>
        /// The next block to store
        /// </summary>
        public static ulong CurrentBlock = 0uL;
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
        public static decimal PartialInstall(byte[] system, Disk disk, uint partition)
        {
            if (Done && system.Length == SystemSize) //Check if it is already completed
            {
                return -1;
            }
            else if (Done && system.Length != SystemSize) //Reset the variables in case its already done and another OS will be installed
            {
                Done = false;
                PartialInstallAvailable = true;
            }
            else if (!Done && system.Length != SystemSize && !PartialInstallAvailable) //Check if its not done yet but the 'system' byte array was changed
            {
                throw new DataMisalignedException("The specified system bytes are not the array used originally");
            }
            if (PartialInstallAvailable)
            {
                //Make some checks before installing:
                if (system == null || system.Length < 1) //Check if the system bytes exist
                {
                    throw new ArgumentNullException("system");
                }
                if (disk.IsMBR) //Check if the disk is GPT
                {
                    throw new FormatException("The disk is not GPT!");
                }
                if (partition > disk.Partitions.Count - 1) //Check if the partition exists
                {
                    throw new ArgumentOutOfRangeException("partition");
                }
                if (disk.Partitions[(int)partition].Host.BlockCount * disk.Partitions[(int)partition].Host.BlockSize < (ulong)system.Length + (64 * 512)) //Check if the partition has enough space for the system (plus 32KB just in case)
                {
                    throw new Exception("Not enough space in the partition");
                }
                PartialInstallAvailable = false;
                SystemSize = system.Length;
                CurrentBlock = 0uL;
                Index = 0;
            }

            //Installing process:
            byte[] block = new byte[64 * 512]; //Create a byte array to store 32KB chunks
            ulong index = 0; //Create a ulong to store the current index in the 'block' byte array
            ulong currentBlock = CurrentBlock; //Create a ulong to keep track of the current data block
            for (ulong i = (ulong)Index; i < (ulong)system.Length; i++, index++, Index++)
            {
                if (index == 64 * 512) //Code to execute when the 'block' byte array is full
                {
                    index = 0; //Reset the byte index
                    disk.Partitions[(int)partition].Host.WriteBlock(currentBlock, 64uL, ref block); //Write the data block in the partition
                    Array.Clear(block); //Clear the 'block' byte array
                    CurrentBlock += 64; //Skip 64 blocks
                    Heap.Collect(); //Run GC Collection to prevent the kernel from crashing
                    break;
                }
                block[index] = system[i]; //Store the current system byte in the 'block' byte array
            }
            if (Done) //Returns the percentage of the installation process and -1 in case its finished
            {
                return -1;
            }
            else
            {
                return (decimal)Index / SystemSize * 100;
            }
        }
    }
}
