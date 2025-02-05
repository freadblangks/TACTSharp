# TACTSharp
Library utilizing memory-mapped file access to speed up initial loading and have lower RAM usage compared to other libraries that load WoW's CASC storage. Made so Marlamin can extract things on their NAS without it getting resource starved.  
Largely based on @bloerwald's memory-mapped file reading implementations.

## Current features
- Faster build loading time and lower memory footprint compared to other solutions.
- Loads both online and local World of Warcraft installations.
- Loading of game files by ekey, ckey, ID or by name from install/root manifest or listfile (only loaded if name not in install/root).

## TODO for 1.0
- Stabilize and lock-in API usage.
- Support for encrypted products.
- Support for specifying install tag priority (so e.g. the exe for WoW China can be extracted).
- Ability to use a folder with CDN-structured files as a data source (similar to local WoW installs).
- Test run on all available WoW CDN data to data starting at 6.0.
- Automated tests.

## Not planned
This may change in the future, but this is currently **NOT** planned to be part of 1.0.
- Support for other games than WoW.
- GUI tool.

# TACTTool
CLI tool using the library for simple file extraction. 

## Usage
```
Description:
  TACTTool - Extraction tool using the TACTSharp library

Usage:
  TACTTool [options]

Options:
  -b, --buildconfig <buildconfig>  Build config to load (hex or file on disk)
  -c, --cdnconfig <cdnconfig>      CDN config to load (hex or file on disk)
  -p, --product <product>          TACT product to load [default: wow]
  -r, --region <region>            Region to use for patch service/build selection/CDNs [default: us]
  -l, --locale <locale>            Locale to use for file retrieval [default: enUS]
  -m, --mode <mode>                Input mode: list, ekey (or ehash), ckey (or chash), id (or fdid), name (or filename)
  -i, --inputvalue <inputvalue>    Input value for extraction
  -o, --output <output>            Output path for extracted files, folder for list mode (defaults to 'extract' folder), output filename for other input modes (defaults
                                   to input value as filename)
  -d, --basedir <basedir>          WoW installation folder to use as source for build info and read-only file cache (if available)
  --version                        Show version information
  -?, -h, --help                   Show help and usage information
```

### Examples
#### FileDataID
You can use either `fdid` or `id` modes to extract specific FileDataIDs from the client.
- `TACTTool -m fdid -i 53188` extracts FileDataID 53188 from Retail WoW to `./53188`
- `TACTTool -m fdid -i 53188 -o "warrior terrace.mp3"` extracts FileDataID 53188 from Retail WoW to `./warrior terrace.mp3`

#### Filename
You can use either `name`, `filename` or `install` modes to extract files from the `install` manifest based on filename. 
- `TACTTool -m name -i Wow.exe` extracts Wow.exe from Retail WoW `./Wow.exe `
- `TACTTool -p wowt -m name -i WowT.exe` extracts WowT.exe from WoW PTR to `./WowT.exe `

#### EKey/CKey
You can use either `ekey` or `ehash` to extract files by their EKey or you can use `ckey` or `chash` to extract files by their CKey.
- `TACTTool -m ckey -i 7fa7faa0fdece01a838af3c4005f7f69` extracts CKey `7fa7faa0fdece01a838af3c4005f7f69` to `./7fa7faa0fdece01a838af3c4005f7f69`
- `TACTTool -m ckey -i 7fa7faa0fdece01a838af3c4005f7f69 -o BattlePetAbilityEffect.db2` extracts CKey `7fa7faa0fdece01a838af3c4005f7f69` to `./BattlePetAbilityEffect.db2`

#### List
- `TACTTool -m list -i pathtolist.txt` will read `pathtolist.txt` and extract files to the `extract` folder in the current working directory.
- `TACTTool -m list -i pathtolist.txt -o D:/myoutput` will read `pathtolist.txt` and extract files to the `D:/myoutput` folder.

Put files to extract in a plain text file in the below format. Similar modes as above are supported. Not giving a mode or FDID assumes it is using the `filename` mode. An additional part can be used to set the output filename.  
These are all valid lines:
```
install;WoW.exe
install;Wow.exe;Wow-but-I-wanted-to-rename-it.exe
WoW.exe
Wow.exe;Wow-maybe-this-name-is-better.exe
ckey;7fa7faa0fdece01a838af3c4005f7f69
ckey;7fa7faa0fdece01a838af3c4005f7f69;BattlePetAbilityEffect.db2
ekey;00b797ea435427185f33466495855656
ekey;00b797ea435427185f33466495855656;SomeFile.bytes
801575;DBFilesClient/BattlePetAbilityEffect.db2
801575;BattlePetAbilityEffect.db2
```

# Support
Only tested on WoW, other products not supported.
