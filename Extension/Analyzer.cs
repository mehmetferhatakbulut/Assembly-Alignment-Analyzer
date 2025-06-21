using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DProcess = System.Diagnostics.Process;
using Task = System.Threading.Tasks.Task;

namespace Extension
{
	internal sealed class Analyzer
	{
		public const int CommandId = 0x0100;

		private const string NONE = "";
		private const string NOP1m = "90h";
		private const string NOP2m = "66h, 90h";
		private const string NOP3m = "0Fh, 1Fh, 00h";
		private const string NOP4m = "0Fh, 1Fh, 40h, 00h";
		private const string NOP5m = "0Fh, 1Fh, 44h, 00h, 00h";
		private const string NOP6m = "66h, 0Fh, 1Fh, 44h, 00h, 00h";
		private const string NOP7m = "0Fh, 1Fh, 80h, 00h, 00h, 00h, 00h";
		private const string NOP8m = "0Fh, 1Fh, 84h, 00h, 00h, 00h, 00h, 00h";
		private const string NOP9m = "66h, 0Fh, 1Fh, 84h, 00h, 00h, 00h, 00h, 00h";
		private const string NOP10m = "66h, 66h, 0Fh, 1Fh, 84h, 00h, 00h, 00h, 00h, 00h";
		private const string NOP11m = "66h, 66h, 66h, 0Fh, 1Fh, 84h, 00h, 00h, 00h, 00h, 00h";
		private const string NOP12m = "66h, 66h, 66h, 66h, 0Fh, 1Fh, 84h, 00h, 00h, 00h, 00h, 00h";
		private const string NOP13m = "66h, 66h, 66h, 66h, 66h, 0Fh, 1Fh, 84h, 00h, 00h, 00h, 00h, 00h";
		private const string NOP14m = "66h, 66h, 66h, 66h, 66h, 66h, 0Fh, 1Fh, 84h, 00h, 00h, 00h, 00h, 00h";
		private const string NOP15m = "66h, 66h, 66h, 66h, 66h, 66h, 66h, 0Fh, 1Fh, 84h, 00h, 00h, 00h, 00h, 00h";

		private const string NOP1c = "0x90";
		private const string NOP2c = "0x66, 0x90";
		private const string NOP3c = "0x0F, 0x1F, 0x00";
		private const string NOP4c = "0x0F, 0x1F, 0x40, 0x00";
		private const string NOP5c = "0x0F, 0x1F, 0x44, 0x00, 0x00";
		private const string NOP6c = "0x66, 0x0F, 0x1F, 0x44, 0x00, 0x00";
		private const string NOP7c = "0x0F, 0x1F, 0x80, 0x00, 0x00, 0x00, 0x00";
		private const string NOP8c = "0x0F, 0x1F, 0x84, 0x00, 0x00, 0x00, 0x00, 0x00";
		private const string NOP9c = "0x66, 0x0F, 0x1F, 0x84, 0x00, 0x00, 0x00, 0x00, 0x00";
		private const string NOP10c = "0x66, 0x66, 0x0F, 0x1F, 0x84, 0x00, 0x00, 0x00, 0x00, 0x00";
		private const string NOP11c = "0x66, 0x66, 0x66, 0x0F, 0x1F, 0x84, 0x00, 0x00, 0x00, 0x00, 0x00";
		private const string NOP12c = "0x66, 0x66, 0x66, 0x66, 0x0F, 0x1F, 0x84, 0x00, 0x00, 0x00, 0x00, 0x00";
		private const string NOP13c = "0x66, 0x66, 0x66, 0x66, 0x66, 0x0F, 0x1F, 0x84, 0x00, 0x00, 0x00, 0x00, 0x00";
		private const string NOP14c = "0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x0F, 0x1F, 0x84, 0x00, 0x00, 0x00, 0x00, 0x00";
		private const string NOP15c = "0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x0F, 0x1F, 0x84, 0x00, 0x00, 0x00, 0x00, 0x00";

		// MASM listings' symbol tables start at line 12. Preceding that is information about MASM, when code listings are disabled through .NOLIST
		private const int MASM_LISTING_SYMBOLTABLE_START_LINE = 12;

		public static readonly Guid CommandSet = new Guid("bdc1a0e4-6fde-4979-91cb-e2f608d6cdbd");

		private static readonly string tempLocation = Environment.GetEnvironmentVariable("TEMP") + @"\ASMALIGN";

		private static readonly string tempfile = "tempfile";

		private readonly DProcess assemble = new DProcess
		{
			StartInfo = new ProcessStartInfo
			{
				WorkingDirectory = tempLocation,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				CreateNoWindow = true
			}
		};

		private readonly string[][] nops =
		{
			new[] // c syntax
			{
				NONE, NOP1c, NOP2c, NOP3c, NOP4c, NOP5c, NOP6c, NOP7c, NOP8c, NOP9c, NOP10c, NOP11c, NOP12c, NOP13c, NOP14c, NOP15c
			},
			new[] // masm syntax
			{
				NONE, NOP1m, NOP2m, NOP3m, NOP4m, NOP5m, NOP6m, NOP7m, NOP8m, NOP9m, NOP10m, NOP11m, NOP12m, NOP13m, NOP14m, NOP15m
			}
		};

		private readonly AsyncPackage package;

		private readonly string[] statements =
		{
			// if GNU assembler is selected, index+1 will be applied to select the corresponding valid instruction.

			"dd ", ".long ", // get address of label
			"\nsection .text\n", "\n.section .text\n", // split label in case instructions are not enclosed in a section
			"db ", ".byte " // NOP suggestion initial define statement
		};

		private Analyzer(AsyncPackage package, OleMenuCommandService commandService)
		{
			this.package = package ?? throw new ArgumentNullException(nameof(package));
			commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

			var menuCommandID = new CommandID(CommandSet, CommandId);
			var menuItem = new MenuCommand(Execute, menuCommandID);
			commandService.AddCommand(menuItem);
		}

		public static Analyzer Instance { get; private set; }

		private static DTE2 DTEInstance { get; set; }

		public static async Task InitializeAsync(ExtensionPackage package)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
			DTEInstance = await package.GetServiceAsync(typeof(DTE)) as DTE2;
			Assumes.Present(DTEInstance);

			if (!Directory.Exists(tempLocation))
				Directory.CreateDirectory(tempLocation);

			// Get options to avoid using default values if the user hasn't changed them right before running the extension. But were changed a while ago.
			package.GetDialogPage(typeof(GeneralOptions));
			package.GetDialogPage(typeof(AssemblyOptions));

			var commandService =
				await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
			Instance = new Analyzer(package, commandService);
		}

		private Tuple<int, int> SetAssembler(int masmSyntax, int gnu)
		{
			switch (AssemblyOptions.assembler)
			{
				case AssemblyOptions.Assembler.NetwideAssembler:
					assemble.StartInfo.FileName = AssemblyOptions.NasmPath.Length > 0
						? Path.GetFullPath(AssemblyOptions.NasmPath) // ensure path is not empty to avoid errors.
						: "nasm.exe";
					assemble.StartInfo.Arguments =
						$"-f bin -l {tempfile}.lst -o {tempfile}.bin {tempfile}.s {AssemblyOptions.AdditionalArguments}";
					break;

				case AssemblyOptions.Assembler.YASM:
					assemble.StartInfo.FileName = AssemblyOptions.YasmPath.Length > 0
						? Path.GetFullPath(AssemblyOptions.YasmPath)
						: "yasm.exe";
					assemble.StartInfo.Arguments =
						$"-f bin -L nasm -l {tempfile}.lst -o {tempfile}.bin {tempfile}.s {AssemblyOptions.AdditionalArguments}";
					break;

				case AssemblyOptions.Assembler.GNUAssembler:
					masmSyntax = 0;
					gnu = 1;
					assemble.StartInfo.FileName = AssemblyOptions.GasPath.Length > 0
						? Path.GetFullPath(AssemblyOptions.GasPath)
						: "as.exe";
					assemble.StartInfo.Arguments = $"-al -o {tempfile}.bin {tempfile}.s {AssemblyOptions.AdditionalArguments}";
					break;

				case AssemblyOptions.Assembler.MacroAssembler:
					masmSyntax = 1;
					assemble.StartInfo.FileName = AssemblyOptions.MasmPath.Length > 0
						? Path.GetFullPath(AssemblyOptions.MasmPath)
						: "ml.exe";
					assemble.StartInfo.Arguments =
						$"{AssemblyOptions.AdditionalArguments} /nologo /Fo\"{tempfile}.bin\" /Fl\"{tempfile}.lst\" /c {tempfile}.s";
					break;

				case AssemblyOptions.Assembler.MacroAssembler64:
					masmSyntax = 1;
					assemble.StartInfo.FileName = AssemblyOptions.MasmPath.Length > 0
						? Path.GetFullPath(AssemblyOptions.MasmPath)
						: "ml64.exe";
					assemble.StartInfo.Arguments =
						$"{AssemblyOptions.AdditionalArguments} /nologo /Fo\"{tempfile}.bin\" /Fl\"{tempfile}.lst\" /c {tempfile}.s";

					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			return new Tuple<int, int>(masmSyntax, gnu);
		}

		private async void writeToTemporaryASMFile(int gnu, int line, int endLine, int topColumn, EditPoint editPoint,
			string Label)
		{
			using (var writer = new StreamWriter(File.Create($@"{tempLocation}\{tempfile}.s")))
			{
				if (AssemblyOptions.assembler == AssemblyOptions.Assembler.MacroAssembler64 ||
								AssemblyOptions.assembler == AssemblyOptions.Assembler.MacroAssembler)
					await writer
						.WriteLineAsync(".NOLIST"); // it's impossible to place a db outside a section in MASM. Instead, code listing is disabled for faster lookup through the symbol table.
				else
					// get corresponding define statement to get the label's value
					// section added to split the definition from instructions outside a section.
					await writer.WriteLineAsync(statements[gnu] + Label + statements[gnu + 2]);

				for (var i = 1; i <= endLine; i++)
				{
					var s = editPoint.GetLines(i, i + 1); // individual lines read for memory efficiency.

					if (i == line)
						await writer.WriteLineAsync(s.Insert(topColumn, $"\n{Label}:\n"));
					else
						await writer.WriteLineAsync(s);
				}
			}
		}

		private async Task<string> parseFile(int gnu, string Label)
		{
			var HexValue = "";
			var str = "";
			var listingLocation = tempLocation + $@"\{tempfile}.lst";

			switch (AssemblyOptions.assembler)
			{
				case AssemblyOptions.Assembler.NetwideAssembler:
					// definition statement is in the first line of a NASM listing.
					str = File.ReadLines(listingLocation).ElementAt(0);

					var startIndex = str.IndexOf('[');
					var S = str.Substring(startIndex + 1, 8);

					HexValue =
						$"{S[6]}{S[7]}{S[4]}{S[5]}{S[2]}{S[3]}{S[0]}{S[1]}"; // Convert the value from little endian to big endian.
					return HexValue;

				case AssemblyOptions.Assembler.YASM:
					// definition statement is in the second line of a YASM listing.
					str = File.ReadLines(listingLocation).ElementAt(1);
					var next = false;

					foreach (var s in str.Split())
						if (s.Length == 8) // 2 consecutive integers in the same line have the length of 8.
						{
							if (next == false)
							{
								next = true;
								continue;
							}

							HexValue = $"{s[6]}{s[7]}{s[4]}{s[5]}{s[2]}{s[3]}{s[0]}{s[1]}";
							return HexValue;
						}

					return null;

				case AssemblyOptions.Assembler.GNUAssembler:
					while
						// Get rid of the space at the end, GAS listing files don't include that for some reason.
						(!str.Contains(statements[gnu].TrimEnd()) && !assemble.StandardOutput.EndOfStream)
						str = await assemble.StandardOutput.ReadLineAsync();

					foreach (var s in str.Split())
						if (s.Length == 8)
						{
							HexValue = $"{s[6]}{s[7]}{s[4]}{s[5]}{s[2]}{s[3]}{s[0]}{s[1]}";
							return HexValue;
						}

					DTEInstance.StatusBar.Text = "Error trying to parse the listing of GNU Assembler.";
					return null;

				case AssemblyOptions.Assembler.MacroAssembler:
				case AssemblyOptions.Assembler.MacroAssembler64:
					var readLines = File.ReadLines(listingLocation);
					var listingLines = readLines.Count();

					for (var lines = MASM_LISTING_SYMBOLTABLE_START_LINE;
										!str.Contains(Label) && lines < listingLines;
										lines++)
						str = readLines.ElementAt(lines);

					foreach (var s in str.Split())
						if (s.Length == 8)
						{
							HexValue = s;
							return HexValue;
						}

					DTEInstance.StatusBar.Text =
						"Error trying to parse the listing of Macro Assembler. If you have /Sn in the additional options, or .NOCREF in the assembly code, then try removing them.";
					return null;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private async void Execute(object sender, EventArgs e)

		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

			var desiredAlignmentBoundary =
				GeneralOptions.desiredAlignmentBoundary - 1; // - 1 for bitwise AND or Modulo by a power of 2
			var Label = AssemblyOptions.Label;
			var assumedalignment = GeneralOptions.assumedAlignment;
			var maximumNOPSize = GeneralOptions.maximumNopSize;
			var masmSyntax = Convert.ToInt32(GeneralOptions.MasmStyle);
			var gnu = 0;

			(masmSyntax, gnu) = SetAssembler(masmSyntax, gnu);

			var activeDocument = DTEInstance?.ActiveDocument;
			var documentSelection = activeDocument?.Selection as TextSelection;

			// Get the starting point of selection, as that's where the alignment will be checked and the suggestion will be inserted.
			if (!(documentSelection?.TopPoint is VirtualPoint topPoint))
			{
				DTEInstance.StatusBar.Text = "Error: No cursor position found.";
				return;
			}

			var topColumn = string.IsNullOrEmpty(documentSelection.Text.Trim()) ? 0 : topPoint.LineCharOffset - 1;
			var line = topPoint.Line;

			var textdoc = (TextDocument)activeDocument?.Object();
			var editPoint = textdoc.StartPoint.CreateEditPoint();
			var endLine = textdoc.EndPoint.Line;

			if (activeDocument?.Language == "C/C++")
			{
				DTEInstance.StatusBar.Text = "C/C++ support hasn't been added yet. ";
				//TODO: IMPLEMENT SUPPORT FOR C/C++
				return;
			}

			if (!activeDocument.Name.EndsWith(".s") && !activeDocument.Name.EndsWith(".asm"))
			{
				DTEInstance.StatusBar.Text = "Invalid File Extension. This extension supports C/C++/ASM only.";
				return;
			}

			writeToTemporaryASMFile(gnu, line, endLine, topColumn, editPoint, Label);

			var exception = false;
			await Task.Run(() =>
			{
				try
				{
					assemble.Start();
					assemble.WaitForExit();
				}
				catch (Exception)
				{
					exception = true;
				}
			});

			if (exception || assemble.ExitCode != 0)
			{
				DTEInstance.StatusBar.Text =
					"Error: Assembling failed. Make sure you have your assembler in your PATH environment variable, your assembler path is valid or your assembly code is entirely valid.";
				return;
			}

			var HexValue = await parseFile(gnu, Label);
			if (string.IsNullOrEmpty(HexValue)) return;

			// When bitwise AND'ing with a number of a power of 2 minus 1, a modulo operation is done.
			// + 1 is added to ensure that the result is 0-31, not -1-30.
			// Could also be expressed as (boundary - (address % boundary)) % boundary
			var result =
				(desiredAlignmentBoundary + 1 - (int.Parse(HexValue, NumberStyles.HexNumber) & desiredAlignmentBoundary)) &
				desiredAlignmentBoundary;

			var temporary = result; // temporary variable to display on the status bar. Because result is used.
			if (result == 0)
			{
				DTEInstance.StatusBar.Text = "No Alignment Needed.";
				return;
			}

			var nopamount = 1;
			var noptocopy = new StringBuilder(statements[gnu + 4]); // set the initial text into "db " or ".byte ".
			while (result > maximumNOPSize)
			{
				result -= maximumNOPSize;
				noptocopy.Append(nops[masmSyntax][maximumNOPSize]);

				if (result != 0)
				{
					noptocopy.Append(",  ");
					nopamount++;
				}
			}

			noptocopy.Append(nops[masmSyntax][result]);

			DTEInstance.StatusBar.Text = $"{temporary} byte(s) needed for alignment. {nopamount} NOP(s) inserted.";

			var indentation = new string(editPoint.GetLines(line, line + 1).TakeWhile(char.IsWhiteSpace).ToArray());

			editPoint.MoveToLineAndOffset(line, topColumn + 1);

			if (topColumn == 0)
				editPoint.Insert($"{indentation}{noptocopy}\n");
			else
				editPoint.Insert(
					$"\n{indentation}{noptocopy}\n{indentation}"); // Indentation at the end and a newline at the start is needed if a certain part of line is selected.
		}

		public class GeneralOptions : DialogPage
		{
			public static int assumedAlignment;
			public static int desiredAlignmentBoundary = 16;
			public static bool MasmStyle;
			public static int maximumNopSize = 9;

			[Category("General")]
			[DisplayName("Assumed Initial Alignment")]
			[Description("0 by default, sets the assumed starting alignment for the assembly program.")]
			[DefaultValue(0)]
			public int assumedalignmentValue
			{
				get => assumedAlignment;
				set => assumedAlignment = Math.Abs(value) & (desiredAlignmentBoundary - 1);
			}

			[Category("General")]
			[DisplayName("Desired Alignment Boundary")]
			[Description("16 by default. Has to be a power of 2. Sets the desired alignment boundary.")]
			[DefaultValue(16)]
			public int desiredAlignmentBoundaryValue
			{
				get => desiredAlignmentBoundary;
				set
				{
					var logarithm = Math.Abs(Math.Log(value, 2));
					desiredAlignmentBoundary = 1 << (int)Math.Round(logarithm); // Get nearest power of 2, in case input is wrong.

					// modulo the old assumed alignment with the new desired alignment boundary. Given the fact the set method basically takes the modulo of assumed alignment.
					assumedalignmentValue = assumedalignmentValue;
				}
			}

			[Category("General")]
			[DisplayName("Masm Style Syntax")]
			[Description(
				"False by default. If true, the padding will use 'h' as a postfix; if false, the padding will use '0x' as a prefix for the bytes. (Ignored for MASM and GAS)")]
			[DefaultValue(false)]
			public bool masmStyleValue
			{
				get => MasmStyle;
				set => MasmStyle = value;
			}

			[Category("General")]
			[DisplayName("Maximum NOP Size")]
			[Description(
				"9 by default due to encouragement of Intel's documentations. Sets the maximum NOP size/bytes if alignment is over the set value. (0-15)")]
			[DefaultValue(9)]
			public int maximumNopSizeValue
			{
				get => maximumNopSize;
				set => maximumNopSize = value & 15;
			}
		}

		public class AssemblyOptions : DialogPage
		{
			public enum Assembler
			{
				NetwideAssembler,
				GNUAssembler,
				YASM,
				MacroAssembler,
				MacroAssembler64
			}

			public static string Label = "__ASM_ALIGNER_CHECK_FOR_ALIGNMENT_SUGGEST_NOP__";
			public static string AdditionalArguments = "";
			public static Assembler assembler = Assembler.NetwideAssembler;
			public static string NasmPath = "";
			public static string YasmPath = "";
			public static string GasPath = "";
			public static string MasmPath = "";

			[Category("Assembly")]
			[DisplayName("Label")]
			[Description(
				"The label to use for the assembly code. (Only change if you utilize a label with the exact same name)")]
			[DefaultValue("__ASM_ALIGNER_CHECK_FOR_ALIGNMENT_SUGGEST_NOP__")]
			public string labelValue
			{
				get => Label;
				set => Label = value;
			}

			[Category("Assembly")]
			[DisplayName("Assembler")]
			[Description("The assembler to use for the assembly syntax. (NASM by default)")]
			[DefaultValue(Assembler.NetwideAssembler)]
			[TypeConverter(typeof(EnumConverter))]
			public Assembler assemblerValue
			{
				get => assembler;
				set => assembler = value;
			}

			[Category("Assembly")]
			[DisplayName("Additional Arguments")]
			[Description("Additional arguments to pass to the assembler. (Leave empty for none)")]
			[DefaultValue("")]
			public string additionalArguments
			{
				get => AdditionalArguments;
				set => AdditionalArguments = value;
			}

			[Category("Paths")]
			[DisplayName("NetwideAssembler Full Path")]
			[Description(
				@"If NASM's Full Path is not in your PATH environment variable, specify the full path here. Example: C:\Program Files (x86)\NASM\nasm.exe")]
			[DefaultValue("")]
			public string nasmPathValue
			{
				get => NasmPath;
				set => NasmPath = File.Exists(value) ? value : "";
			}

			[Category("Paths")]
			[DisplayName("YASM Full Path")]
			[Description(
				@"If YASM's parent folder is not in your PATH environment variable, specify the full path here. Example: C:\Program Files (x86)\YASM\yasm.exe")]
			[DefaultValue("")]
			public string yasmPathValue
			{
				get => YasmPath;
				set => YasmPath = File.Exists(value) ? value : "";
			}

			[Category("Paths")]
			[DisplayName("GNUAssembler Full Path")]
			[Description(
				@"If GAS' parent folder is not in your PATH environment variable, specify the full path here. Example: C:\w64devkit\bin\as.exe")]
			[DefaultValue("")]
			public string gasPathValue
			{
				get => GasPath;
				set => GasPath = File.Exists(value) ? value : "";
			}

			[Category("Paths")]
			[DisplayName("MacroAssembler Full Path")]
			[Description(
				@"If MASM's parent folder is not in your PATH environment variable, specify the full path here. Example: C:/Program Files/Microsoft Visual Studio/2022/Preview/VC/Tools/MSVC/14.44.35207/bin/HostX64/x64/ml64.exe")]
			[DefaultValue("")]
			public string masmPathValue
			{
				get => MasmPath;
				set => MasmPath = File.Exists(value) ? value : "";
			}
		}
	}
}
