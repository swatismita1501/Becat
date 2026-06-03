# CAT port status

This package includes the next concrete migration step for the Paradox `stdcat.lsl` port.

Implemented in this package:

- Paradox `stdcat.txt` added under `ParadoxReference/`.
- No Paradox output ZIPs or DB templates are embedded in the application package
- `StdcatFormatter.cs` added with direct C# ports of the shared `stdcat.lsl` helper routines:
  - `remLzero`
  - `deDash`
  - `deSpace`
  - `spacepadStr`
  - `zeropadStr`
  - `intfill`
  - `catnumascii`
  - `tosize`
  - `castNumCobol`
  - `massageUpc`
  - `massageGtin`
  - `checkGTIN`
  - `ComputeGTINCheckDigit`
  - `addcentury`
  - `ReformatDate`
  - `NormFinecode`
- `StdcatExportLayout.cs` added with fixed-width export layout definitions derived from the Paradox export templates and validated record lengths:
  - `ITEM.TXT` = 1806 bytes per row before CR/LF
  - `VEND.TXT` = 439 bytes per row before CR/LF
  - `DEPT.TXT` = 54 bytes per row before CR/LF
  - `CLAS.TXT` = 56 bytes per row before CR/LF
  - `FINE.TXT` = 60 bytes per row before CR/LF
- `JobSetupService.cs` now mirrors the Paradox setup form more closely for CAT jobs:
  - `JobID` still derives from `WhlCode + CatMonth`
  - `FromDate` is now inferred from an existing catalog item source file when one is already present under the configured CAT job folder
  - the lookup checks both configured archive/raw roots so migrated environments do not silently lose the date hint
- `JobFileContractService.cs` now stages the matched source files into the local work directory before downstream processing:
  - CAT, EFM, INH, and HHHAPR jobs now copy the validated input pack into the working folder using the original source file names
  - non-HHH CAT raw source files now populate `ITEM_RAW.DB`, `UPC_RAW.DB`, and `ITEMEXT.DB` from the real matched inputs instead of placeholder text
  - in-house raw source files now populate `ITEM_RAW.DB`, `UPC_RAW.DB`, `VEND_RAW.DB`, `ITEMEXT.DB`, `DERAW.DB`, `CLRAW.DB`, and `FIRAW.DB` from the real matched inputs when those inputs exist
  - Paradox seed table templates now populate `ITEM.DB`, `DEPT.DB`, `CLAS.DB`, `FINE.DB`, `VEND.DB`, and the in-house `<WHL>_I.DB` staging table so those files are no longer synthetic placeholders
- `StdcatCatEngine.cs` now includes a real House-Hasson CAT transformation pass when a transformation logic runs:
  - parses staged `HHH_I.DAT`, `HHH_LDESC.DAT`, `HHH_VM.DAT`, `HHH_DE.DAT`, `HHH_CL.DAT`, and `HHH_FI.DAT`
  - derives distinct dept, class, fine, and vendor export rows from the parsed item records instead of copying raw input text straight through
  - generates fixed-width `ITEM.TXT`, `VEND.TXT`, `DEPT.TXT`, `CLAS.TXT`, and `FINE.TXT` files from parsed values, long-description chunks, and joined lookup data
- `StdcatCatEngine.cs` now also includes a generic non-HHH CAT parsing/export path:
  - parses staged item input lines into catalog rows with derived dept/class/fine/vendor codes
  - merges staged UPC input by SKU when present
  - merges staged long-description input by SKU when present
  - generates non-placeholder `ITEM.TXT`, `VEND.TXT`, `DEPT.TXT`, `CLAS.TXT`, and `FINE.TXT` files for non-HHH CAT jobs
- `StdcatCatEngine.cs` now also includes an EJD-specific CAT parsing/export path:
  - parses staged `ejd_i.dat`, `ejd_ldesc.dat`, `ejd_de.dat`, `ejd_cl.dat`, and `ejd_vm.dat`
  - maps EJD item output from the fixed-width source fields through explicit Paradox-style stages named for `itemIntoOut`, `StdVndOut`, `makeecde`, `makeeccl`, and `makeecfi`
  - derives vendor, department, and class outputs from the staged EJD source pack using that Paradox-style flow instead of the generic regex fallback
- vendor master input files are now optional across wholesaler profiles:
  - when present, vendor rows use the joined vendor master details
  - when absent, validation no longer blocks the job and the CAT transformation emits fallback vendor rows keyed from item data
- generated CAT exports now handle Windows case-insensitive filenames safely:
  - the helper no longer tries to copy `ITEM.TXT` to `item.txt` as if they were different files
  - case-only export renames now go through a temporary file so `item.txt`, `dept.txt`, `clas.txt`, `fine.txt`, and `vend.txt` can be produced reliably on Windows
- `JobFileContractService.cs` now routes `EJD` catalog jobs through the dedicated EJD parser and uses the generic parsed CAT engine for other non-HHH catalog jobs while still copying the generated uppercase exports into the existing lowercase archive names used by the rest of the app

Still remaining before claiming full business-logic parity:

- Full field-for-field parity validation of the new House-Hasson parsers against more than the supplied sample set.
- Wholesaler-specific field maps and validations for the generic/non-HHH CAT source formats.
- Remaining generic/non-HHH direct ports of `itemIntoOut` / `C1_itemproc`.
- Remaining generic/non-HHH direct ports of `StdVndOut` / `C2_vendproc`.
- Remaining generic/non-HHH direct ports of `makeecde`, `makeeccl`, and `makeecfi`.
- Removal of the HHHAPR reference-output shortcut after raw-input generation matches the Paradox output byte-for-byte.

Current safe behavior:

- HHH output is generated by transformation logic; no reference baseline shortcut is used.
- Other HHH months now run through a real parsed CAT transformation path instead of the old raw text pass-through.
- Non-HHH CAT now generates parsed export files from the staged item/UPC/long-description inputs instead of placeholder export text, but this path is still heuristic until each wholesaler format is validated directly.
- INH processing still stages the real source pack and expected working tables, but its business transformations still remain incomplete.
- The project no longer should be treated as a full generic stdcat replacement until the remaining ports above are complete and validated.


## Stored-output and template policy

This build does not ship stored Paradox output ZIPs or Paradox DB templates. Runtime artifacts are generated from staged input files and transformation logic.

## HHH inner archive shape update

HHH CAT runs now create the inner export zip from the job id using a generic lowercase rule, for example `HHHJAN` -> `hhhjan.zip`. The archive folder receives the generated DB artifacts plus that inner zip; standalone TXT exports remain inside the inner zip. This mirrors the richer Paradox archive package shape without embedding month-specific output archives.
