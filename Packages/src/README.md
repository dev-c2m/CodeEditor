# Code Editor

![alt text](https://img.shields.io/badge/Unity-2021.3%2B-blue.svg?logo=unity)
![alt text](https://img.shields.io/badge/Unity-6000.0%2B-blue.svg?logo=unity)  
![alt text](https://img.shields.io/badge/License-MIT-yellow.svg)

[Description](#description) | [Features](#features) | [Installation](#installation) | [Usage](#usage) | [Contributing](#contributing)

## Description
This is a versatile in-game code editor that can be used for various purposes.  
It is built by making the most of Unity's TMP_InputField.  
Shortcuts are implemented using Unity's new Input System.

## Features
- Indent / Unindent (with Multi Line)
- Syntax Highlighting
- Undo / Redo
- Line Numbering
- Auto Complete
- Auto Indent
- Horizontal / Vertical Scroll


## Installation
_This package requires **TextMeshPro**._
### Install via UPM (Unity Package Manager)
- Click `Window > Package Manager` to open Package Manager UI.
- Click `+ > Install(or Add) package from git URL...` and input the repository URL: `https://github.com/dev-c2m/CodeEditor.git?path=Packages/src`  
![Image](https://github.com/user-attachments/assets/d1fd3692-b6b2-44e7-98b2-d2e9c4230cf0)
- To update the package, change suffix `#{version}` to the target version.
    - e.g. `https://github.com/dev-c2m/CodeEditor.git?path=Packages/src#1.0.0`

## Usage
### Getting Started
1. [Install the package.](#-installation-)
2. Download sample  
   ![sample](https://github.com/user-attachments/assets/dc1feba9-f16e-44a4-9f7b-bd2f9545fb34)
3. Drag and Drop prefab in Scene

**``We recommend downloading the sample and using the included prefab.``**

## Setting
### Language  
**Creating the Language Setting**  
`Asset - Create - Code Editor - Language Setting`  
![language](https://github.com/user-attachments/assets/035b5058-80a7-4b43-82a1-e443450c5a14) 
-   **Keywords**: The list of keywords for autocompletion and syntax highlighting.
-   **Keyword Color**: The color used to highlight the keywords.
-   **Sub Keywords**: Sub-keywords that belong to a main keyword. These are often static members or properties that appear after a main keyword (e.g., `Vector3.zero`).
-   **Symbol**: The list of symbols to be highlighted (e.g., `+`, `-`, `*`, `/`).
-   **Line Comment**: The string that marks the beginning of a single-line comment (e.g., `//`).
-   **Multi Line Comment Start**: The string that marks the beginning of a multi-line comment block (e.g., `/*`).
-   **Multi Line Comment End**: The string that marks the end of a multi-line comment block (e.g., `*/`).
-   **Comment Color**: The color used for comments.
-   **Auto Indent Language**: Sets the style for automatic indentation. Currently, only C# is supported.
-   **Indent Type**: Sets the indentation character type: `Tab` or `Space`.
-   **Auto Indent Chars**: Characters that trigger an auto-indent on the next line (e.g., `{`).
-   **Auto UnIndent Chars**: Characters that trigger an auto-unindent on the current line (e.g., `}`).


### Code Editor  
![codeeditor](https://github.com/user-attachments/assets/6dc23713-4f2b-4294-8b01-41d8d1fbc93b)  
**Code Editor Setting**  
- **Language Setting**: The language setting asset created above.
- **undoIdleThresholdSeconds**: If there is no input for this duration (in seconds), the changes are recorded for the undo history.
- **Point Size**: Adjusts the size of all fonts in the editor.


### Auto Complete  
![auto](https://github.com/user-attachments/assets/44576e63-92bd-448a-8d2e-2ed6849198ed)
- **Language Setting**: The language setting asset created above.
- **Max Suggestions**: Specifies the maximum number of auto-complete suggestions. (-1 means no limit).
- **Code Complete Default Color**: The background color for suggested auto-complete keywords.
- **Code Complete Select Color**: The background color for a selected auto-complete keyword.

## Contributing

### Issues
Issues are very helpful for identifying areas that need improvement and can enhance the user experience. They are always welcome!

### Pull Requests
Coming soon.

### Support
> This is an open-source project. If you found it helpful, please leave a star  
![alt text](https://img.shields.io/github/stars/dev-c2m/CodeEditor?style=for-the-badge)

## License

MIT
