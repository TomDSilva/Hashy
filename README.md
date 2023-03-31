# Hashy

Hashy is a .NET 6 WPF Windows application used to detect silent file corruption.
It does this via the creation of a "scan" report initially, and then the user at a later date can use the inbuilt "check" function to compare the report's hash data to current files.

The current design of the UI features on the left side of the app and ability to run an initial scan.
Users have the option to select hash mode(MD5 as default, but also the option for SHA256).
An initial scan will generate a CSV at your chosen location containing:
1. On line 1 a reference ID is used to confirm legitimate reports.
2. On line 2 details of the report such as:
- The date the report was created.
- The full path that was scanned.
- And the hash mode that was used.
3. Line 3 onwards contains details of the files in question, including:
- Location.
- Hash.
- Last modified.

The right side of the app has a checking option which you use to compare a pre-existing report that you have created (see initial scan above) with the current files.
Hashy makes sure to compare modified timestamps of the file to determine if the file has been user modified (as this would also have a different hash).
Any inconsistencies are output to the console window in red.

## Screenshots

### Default view after opening:
![image](https://user-images.githubusercontent.com/20383538/229202580-b6ba4a90-b335-4c03-9f03-1629bf457e30.png)

### During an initial scan:
![image](https://user-images.githubusercontent.com/20383538/229202757-cc1d5578-70c9-4cbb-be4a-c84992537577.png)

### After the initial scan finishes:
![image](https://user-images.githubusercontent.com/20383538/229202834-43871dcb-0b78-447a-ba14-75f6507f41b3.png)

### After the follow up check finishes:
![image](https://user-images.githubusercontent.com/20383538/229202957-8de2c2a2-aaf0-439e-965d-450a71c6223b.png)

### Example CSV file created from initial scan:
![image](https://user-images.githubusercontent.com/20383538/229202491-7bcba0bc-d655-4f4c-99e3-7e6761c5bd30.png)
