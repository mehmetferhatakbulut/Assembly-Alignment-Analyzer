# BETA ! Problems may occur.
# Assembly Alignment Analyzer
A Visual Studio extension to automatically suggest explicit nops/long nops for instruction alignment. 
## Currently Supported and Future Functionalities 
- [x] NASM(Netwide Assembler)
- [x] YASM(Yet Another Assembler)
- [x] GAS(GNU Assembler)
- [x] MASM(Microsoft Macro Assembler)
- [x] MASM64(Microsoft Macro Assembler 64-bit)
- [ ] ARM Assembly
- [ ] C/C++ Inline Assembly
- [ ] C/C++ regular code alignment

## Checking Alignment
Right click / use the Shift + F10 keys to open the context menu. The command is "Check Instruction Alignment".

![image](https://github.com/user-attachments/assets/11b7b8eb-74e5-475a-98dd-42b54d607ca1)

The required NOP(s) will be inserted automatically.

![image](https://github.com/user-attachments/assets/d34a408b-1e5b-4491-8669-040c73f21e4a)

This extension also supports selections, in case there are multiple instructions/pieces of code in a single place.

![image](https://github.com/user-attachments/assets/7b29cfc8-1542-48b3-aa92-04253e0af9bf)

## Caveats / Important Notes
- Make sure to check the alignments from top to bottom. If an alignment for the latter sections of code was done before for the former sections, the alignment suggestion for the sections after the new alignment suggestion will be invalid.
- GAS and MASM ignore the MASM-style definitions setting as they are incompatible with eachother.
- The full path needs to be valid, otherwise it will be deleted. Supports both '\' and '/' as delimiters.
- The temporary folder resides at %TEMP%/ASMALIGN. It's not deleted by the extension because it doesn't take up much space.
- Only checks a single ASM/C/C++ file, not the entire project(since distinct functions at distinct files are usually aligned on their own, but still, an assumed alignment setting is provided for this.)

## Settings
### General Settings

![image](https://github.com/user-attachments/assets/3bc2b435-75e4-4d49-a945-b4eb4bde38c6)


