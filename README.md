# Cosmares
A library that allows Cosmos Kernels to be installed in a GPT HDD plus a few more disk related tools

Inspired by the original [Cosmares](https://github.com/PratyushKing/Cosmares) by [@PratyushKing](https://github.com/PratyushKing)



# Installation

Download it to your Cosmos project trough [Nuget](https://www.nuget.org/packages/Cosmares)

Then add this line of code to the start of your Kernel.cs:
``using Cosmares;``


# Commands

Cosmares comes with 7 commands:

``Setup.Install``, ``Setup.PartialInstall``, ``Setup.GetFS``, ``Setup.CreateGPTpartition``, ``Setup.DeleteGPTpartition``, ``Setup.StoreInformation`` and ``Setup.GetInformation``





- ``Setup.Install(ReadOnlySpan<byte> system, Disk/BlockDevice disk, uint partition, bool useHeapCollect = true, bool useExtraInfo = false);``

  Installs a Cosmos system in the specified Disk at the specified Partition.

  Optional arguments include the use of Heap.Collect while installing (true by default) and the option to include OS Info specified with ``Setup.StoreInformation`` (false by default)


- ``Setup.PartialInstall(ReadOnlySpan<byte> system, Disk/BlockDevice disk, uint partition, bool useHeapCollect = false, bool useExtraInfo = false);``

  Does the same as ``Setup.Install`` only that this one allows making "pauses" between each write of 128 blocks.
  On each pause it returns a decimal with the percentage of the installation process. Returns -1 if the installation is done.

  (Both extra arguments are set to false)

- ``Setup.GetFS(Disk/BlockDevice disk, uint partition);``

  Identifies the filesystem in a partition (if available).
  
  Cosmares can currently find these:
   - FAT (FAT12 and FAT16 are seen as the same)
   - FAT32
   - ISO9660
   - Linux EXT (EXT2, EXT3 and EXT4 are seen the same)
   - ExFAT
   - NTFS
   - PlutonFS
     
	 
- ``Setup.CreateGPTpartition(Disk/BlockDevice disk, ulong startingBlock, ulong sizeInMB);``

  Creates a GPT partition.
  
  Be careful when choosing the starting block and the size, an exception will be thrown if it overlaps an already existing partition.
  
  This command will use the first available entry rather than the last one, an uint will be returned with the used entry.

- ``Setup.DeleteGPTpartition(Disk/BlockDevice disk, uint partitionEntry);``

  Removes a GPT partition. (Note: Removing a partition does NOT erase its contents)

- ``Setup.StoreInformation(OSinfo/ReadOnlySpan<byte> extraInfo);``

  Stores extra OS information (like the author's name, OS name, version, etc) for the next OS that will be installed
  
  (In case a byte array is being used, the length must be 300 bytes)

- ``Setup.GetInformation(Disk/BlockDevice disk, uint partition);``
  
  Gets extra information (if available) of a installed OS

# Usage

First, you should have two kernels, Your OS and Your OS Installer.

Get the byte array of your OS ISO into your Installer using [VFS](https://github.com/CosmosOS/Cosmos/blob/master/Docs/articles/Kernel/VFS.md) or [ManifestResourceStream](https://github.com/CosmosOS/Cosmos/blob/master/Docs/articles/Kernel/ManifestResouceStream.md).

You will also need a ``Disk`` variable, you can get one using VFS (for example: ``fs.Disks[0]``) (Cosmares is also compatible with ``Disk`` variables generated with [COR-ET's AHCI/SATA library](https://github.com/COR-ET/CORNEL_AHCI_INIT))  ``Note: if for some reason you cannot use a 'Disk' variable, 'BlockDevice' works as well``

Now that we have all that set, we can start using Cosmares' commands like ``Setup.Install`` with the variables we made earlier, like this:

``Setup.Install(yourISObytes, yourDisk, thePartitionYouWantToUse);``


So, for some of you, ``Setup.Install`` would be enough, but what about ``Setup.PartialInstall``? What are those "pauses"?
I'll explain them here:

Let's say you have an Installer with GUI, you would probably have code that moves the cursor and keeps the graphics running, when you use ``Setup.Install`` the system freezes until the installation finishes, which is not good if you want to display a progress bar or have other stuff that must be running, that's when ``Setup.PartialInstall`` comes into place, unlike ``Setup.Install``, when you run it, it only stores 128 data blocks, and then the function ends, in that state you can run any code you want, to finish the installation you just have to place ``Setup.PartialInstall`` in a loop and exit it once the command returns -1. This is also useful when making progress bars, as each time you run it, a decimal with the percentage will be returned.

Cosmares 1.1.0 and higher supports filesystem identification, removal and creation for GPT partitions.

``At the time of writing this README.md, Cosmares does NOT add GPT style on blank disks, it only works on HDDs with GPT already in them``


# Notes
Here are some stuff you should know before using Cosmares:

- Cosmares ONLY works on GPT harddrives with UEFI
- Kernels made with the Userkit will NOT work
- Because of using UEFI your kernel cannot use Console.* commands (like Console.WriteLine, Console.Clear, Console.ReadKey, etc)
- Cosmares is in a Really early version, it may contain bugs, if you find any please report them [here](https://github.com/Gabolate/Cosmares/issues)
- If possible, please enable gzip compression in your system's ISO or else it might use too much RAM in your Installer

If you have come this far, thx for reading, it means a lot, pls leave a star if you found this useful :)  

Made by [@Gabolate](https://github.com/Gabolate) 2024