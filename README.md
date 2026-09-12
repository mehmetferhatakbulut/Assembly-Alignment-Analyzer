###### I am locked out of my old account as I swapped devices without turning off 2FA. I migrated to this account.

https://marketplace.visualstudio.com/items?itemName=ax60.assemblyalignmentanalyzer00001
# This extension is in early release and hasn't been thoroughly tested, errors may occur.
# Assembly Alignment Analyzer
A Visual Studio extension to automatically suggest explicit nops/long nops for instruction alignment. 
## Currently Supported and Future Functionalities 
- [x] NASM(Netwide Assembler)
- [x] YASM(Yet Another Assembler)
- [x] GAS(GNU Assembler)
- [x] MASM(Microsoft Macro Assembler)
- [x] MASM64(Microsoft Macro Assembler 64-bit)
- [ ] C/C++ regular code alignment
- [ ] C/C++ Inline Assembly
- [ ] FASM(?)
- [ ] ARM Assembly(?)

## Checking Alignment
`Right Click` / `Shift + F10` to open the context menu. The command is "Check Instruction Alignment".

![image](https://github.com/user-attachments/assets/11b7b8eb-74e5-475a-98dd-42b54d607ca1)

The required **NOP**(s) will be inserted automatically.

![image](https://github.com/user-attachments/assets/d34a408b-1e5b-4491-8669-040c73f21e4a)

This extension also supports selections, in case there are multiple instructions/pieces of code in a single place.

![image](https://github.com/user-attachments/assets/7b29cfc8-1542-48b3-aa92-04253e0af9bf)

## Settings
### General Settings

![image](https://github.com/user-attachments/assets/46572061-e084-4fc8-94a1-24862e2f737a)

- **Assumed Initial Alignment** : The alignment provided in an assembly listing can be different from one in an application. Therefore it is up to you to determine the initial alignment of the entirety of the function. Can only be set within the range of 0 to **Desired Alignment Boundary - 1**.
- **Desired Alignment Boundary** : Has to be a power of 2. Can range between **1** to **2^31**. Will define the target boundary to align the code in.
- **Masm Style Syntax** : If true, will force the extension to utilize 'h' as a postfix. If false, will force the extension to utilize "0x" as a prefix. Example: db 0x90 / db 90h
- **Maximum NOP Size** : Despite the recommendation by Intel, this option can range between 0-15. Lets you choose the maximum NOP size if the alignment is over the selected value.

---
### Assembly Settings

![image](https://github.com/user-attachments/assets/e01e6357-6cf0-40c1-8e0b-9fb7b90e1937)

- **Additional Arguments** : Lets you utilize custom arguments for the assemblers.(For example: /Ox)
- **Assembler** : Supported assemblers to choose from.

  ![image](https://github.com/user-attachments/assets/7375a053-e66a-46d2-b8d5-e85ebe876eab)

- **Label** : Since this extension utilizes a dummy label, it's recommended to change this if any form of label overlap/multiple labels existing with same names happens.
- **Paths** : Lets the user choose the full paths for assemblers if their parent directories aren't defined inside the PATH environment variable.

### C/C++ Settings

Not yet supported.
  
## Caveats / Important Notes
- Make sure to check the alignments **from top to bottom**. If an alignment for the latter sections of code was done before for the former sections or new instructions were inserted before an alignment, the alignment suggestion for the sections after the new alignment suggestion will be **invalid**.
- **GAS** and **MASM** ignore the MASM-style definitions setting as they are incompatible with eachother.
- The full path needs to be valid, otherwise it will be deleted. Supports both '\\' and '/' as delimiters.
- The temporary folder resides at %TEMP%/ASMALIGN. It's not deleted by the extension because it doesn't take up much space.
- Only checks a single **ASM/C/C++** file, not the entire project(since distinct functions at distinct files are usually aligned on their own, but still, an assumed alignment setting is provided for this.)
- Changing the **Desired Alignment Boundary** setting will result in the **Assumed Initial Alignment** setting to be taken the MOD of the new **Desired Alignment Boundary** value, keeping it within the range. **THE NEW VALUE WON'T BE SHOWN WHILE STILL IN SETTINGS, REOPEN THE SETTINGS TO SEE IT**
