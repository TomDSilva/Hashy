# Hashy

Hashy is a .NET 6 WPF Windows application used to detect silent file corruption.
It does this via it's 2 modes: "Scan Mode" and "Check Mode".
That is the creation of a "scan" report initially, and then the user at a later date can use the inbuilt "check" mode to compare the report's hash data to current files.

## Scan Mode
The current design of the UI features on the left side of Hashy a ability to run an initial scan.
User's have the option to select hash mode (MD5 as default, but also the option for SHA256).
An initial scan will generate a CSV at the user's chosen location containing:
1. On line 1 - a reference ID is used to confirm this report was created directly by Hashy.
2. On line 2 - details of the report such as:
- The date the report was created.
- The full path that was scanned.
- And the hash mode that was used.
3. Line 3 onwards contains details of the files in question, including:
- Location.
- Hash.
- Last modified.

## Check Mode
The right side of Hashy has a checking option which a user can use to compare a pre-existing report that they have created via Hashy (see initial scan above) with the current files.
Hashy makes sure to compare modified timestamps of the file to determine if the file has been user modified (as this would also have a different hash).
Any inconsistencies are output to the console window in red for the user to review.

## Screenshots

### Default view after opening:
![image](https://github.com/TomDSilva/Hashy/assets/20383538/72c523b8-3c03-4487-92ee-fea4201e2321)

### During an initial scan:
![image](https://github.com/TomDSilva/Hashy/assets/20383538/4a8e59df-b5d1-413e-97d4-82f547c45f58)

### After the initial scan finishes:
![image](https://github.com/TomDSilva/Hashy/assets/20383538/18e15db2-c3ee-4597-bc81-0e7a149e8c5b)

### After the follow up check finishes:
![image](https://github.com/TomDSilva/Hashy/assets/20383538/785d7943-5526-4919-a6f2-7d1ee6002d67)

### Example CSV file created from initial scan:
![image](https://user-images.githubusercontent.com/20383538/229202491-7bcba0bc-d655-4f4c-99e3-7e6761c5bd30.png)
