# Multivariate Statistical Analysis
## CAMECA Instruments, Inc.
## Copyright 2024 © CAMECA Instruments, Inc. All rights reserved.

### Development
Clone the repository to a local directory. Open MultivariateStatisticalAnalysis.sln in Visual Studio.

If using AP Suite installed in the default "Just for Me" installation location, simply Run/Debug the extension.
The build destination will be the required extension folder, and the startup application will be AP Suite at the default location.
If not installed to the default location, edit `MultivariateStatisticalAnalysis/Properties/launchSettings.json`
```json
{
  "profiles": {
    "Launch AP Suite": {
      "commandName": "Executable",
      "executablePath": "%LOCALAPPDATA%\\Programs\\CAMECA Instruments\\AP Suite\\Cameca.Shell.Main.exe",
      "workingDirectory": "%LOCALAPPDATA%\\Programs\\CAMECA Instruments\\AP Suite\\"
    },
    "MultivariateStatisticalAnalysis": {
      "commandName": "Project"
    }
  }
}
```
Change "executablePath" and "workingDirectory" to the installation of AP Suite.

If using the standalone IVAS 6 application, in the `launchSettings.json` file, change instances in "executablePath" and "workingDirectory" of "AP Suite" to "IVAS"
```json
{
  "profiles": {
    "Launch AP Suite": {
      "commandName": "Executable",
      "executablePath": "%LOCALAPPDATA%\\Programs\\CAMECA Instruments\\IVAS\\Cameca.Shell.Main.exe",
      "workingDirectory": "%LOCALAPPDATA%\\Programs\\CAMECA Instruments\\IVAS\\"
    },
    "MultivariateStatisticalAnalysis": {
      "commandName": "Project"
    }
  }
}
```

### Using
Open or create an analysis set. Right click Top Level Node, and under "Custom Analysis" select "Multivariate Statistical Analysis".
In the opened panel, click the browse button and select the example voxel->phase APT file. (e.g. `textvoxels.apt`).
Set the voxel dimensions if necesary. The example APT file appeared to have X and Y values flipped, so until that issue is resolved, check "Flip X-Y axis" to correct.
Click "Run" to load the file.
In the Properties panel in the lower left (when the analysis is selected), choose the value of the phase to filter (e.g. '2').
After the data is loaded, this can be don't regardless of if the main panel is open or not.
This custom analysis will filter the ion data to only select ions in the specified phase.
To visualize this, right click the "Multivariate Statistical Analysis" item in the Analysis Tree and select "Open ROI in New Tab"

### Development Considerations
Then main location for additional developement should be in `MultivariateStatisticalAnalysisNode.cs`.
Some comments indicate general targets for further extension.
See `GetIndicesDelegate()` method, which is the filter that selects ions from the parent ion data.