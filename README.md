# Cosmares
A library that allows Cosmos Kernels to be installed in a GPT HDD

Inspired by the original [Cosmares](https://github.com/PratyushKing/Cosmares) by [@PratyushKing](https://github.com/PratyushKing)



# Installation

Download it to your Cosmos project trough [Nuget](nuget.org)

Then add this line of code to the start of your Kernel.cs:
``using Cosmares;``


# Commands

Cosmares comes with two commands, ``Setup.Install`` and ``Setup.PartialInstall``





- ``Setup.Install(byte[] system, Disk disk, uint partition);``

  Installs a Cosmos system in the specified Disk at the specified Partition.



- ``Setup.PartialInstall(byte[] system, Disk disk, uint partition);``

  Does the same as ``Setup.Install`` only that this one allows making "pauses" between each write of 64 blocks.
  On each pause it returns a decimal with the percentage of the installation process. Returns -1 if the installation is done.

# Usage

First, you should have two kernels, Your OS and Your OS Installer.

Get the byte array of your OS ISO into your Installer using [VFS](https://github.com/CosmosOS/Cosmos/blob/master/Docs/articles/Kernel/VFS.md) or [ManifestResourceStream](https://github.com/CosmosOS/Cosmos/blob/master/Docs/articles/Kernel/ManifestResouceStream.md).

You will also need a ``Disk`` variable, you can get one using VFS (for example: ``fs.Disks[0]``) (Cosmares is also compatible with ``Disk`` variables generated with [COR-ET's AHCI/SATA library](https://github.com/COR-ET/CORNEL_AHCI_INIT)

Now that we have all that set, we can start using Cosmares' commands like ``Setup.Install`` with the variables we made earlier, like this:

``Setup.Install(yourISObytes, yourDisk, thePartitionYouWantToUse);``


So, for some of you, ``Setup.Install`` would be enough, but what about ``Setup.PartialInstall``? What are those "pauses"?
I'll explain them here:

Let's say you have an Installer with GUI, you would probably have code that moves the cursor and keeps the graphics running, when you use ``Setup.Install`` the system freezes until the installation finishes, which is not good if you want to display a progress bar or have other stuff that must be running, that's when ``Setup.PartialInstall`` comes into place, unlike ``Setup.Install``, when you run it, it only stores 64 data blocks, and then the function ends, in that state you can run any code you want, to finish the installation you just have to place ``Setup.PartialInstall`` in a loop and exit it once the command returns -1. This is also useful when making progress bars, as each time you run it, a decimal with the percentage will be returned.



If you have come this far, thx for reading, it means a lot, pls leave a star if you found this useful :)  made by [@Gabolate](https://github.com/Gabolate) 2024
