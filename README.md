# YAPC - Yet Another Patch Cleaner
YAPC can clean orphaned .msi/.msp files from the local computer, or remotely on the network.


## How To Use
1. Clone/Download the repository to your machine. (Extract the zip folder)
2. Run YAPC/bin/Release/YAPC.exe
3. Enter the computer you want to clean orphaned windows installer files from
	* If you do not enter a name, the machine running the program is used.
4. YAPC will read the computer registry to look for orphaned files, and then display them. 
5. You now have 2 real choices: **d**elete or **m**ove (or exit).
	* Deletion is permanent. This program deletes files from the `C:/Windows/Installer` directory. If a .msp file is mistakenly deleted,
	future updates and patches may error.
	* You can safely move the files to a separate backup location and restore them if the need arises.


## Why tho?
1. Many computers have multiple drives. Installing Windows on the C: drive, and subsequentally patching the system, will cause the C: drive to fill up over time from orphaned patch files.
2. YAPC keeps the Windows Installer directory small, so that C: can hold more stuff and things.