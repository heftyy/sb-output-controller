# SBOutputController for Sound BlasterX G6
What it can do:
* Assign global hotkeys to switch between sound outputs
* Keep your EqualizerAPO config.txt up to date depending on the sound output
* Run on windows startup

![Image of SBOutputController](https://github.com/heftyy/sb-output-controller/docs/SBOutputController_Main.png)

## How to use:
1. Install Sound Blaster Command
2. Download latest version of SBOutputController from https://github.com/heftyy/sb-output-controller/releases
3. After starting the application will be asked to go through the initial setup

![Image of setup](https://github.com/heftyy/sb-output-controller/docs/SBOutputController_Setup.png)

4. Browse to the directory where Sound Blaster Command and find Creative.SBCommand.exe
(SBOutputController uses the .dlls that are installed with Sound Blaster Command to do the actual output switching and monitoring which output is currently active)
5. Register SndCrUSB.DLL by clicking the Register button in the setup window.
This will run require admin privileges and runs the windows utility regsvr32 to add the dll in the registery. I don't know how to make my application aware of this .dll in other ways, if anybody knows then let me know.
6. Click finish in the setup window.
7. Assign hotkeys.
8. Browse for EqualizerAPO config files.
