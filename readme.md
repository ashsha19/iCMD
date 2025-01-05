# iCMD

iCMD is a .NET Core console application that generates PowerShell or Bash commands based on natural language queries. It uses a locally set up quantized mini LLM (Gemma 2 2.5B model) with LMStudio to generate the commands. After generating the command, it can execute and display the output in the same console window.

I have also tried other mini LLMs like LLAMA 3 and Phi 3.5 but getting decent output from gemma 2.

## Features

- Generate PowerShell or Bash commands from natural language queries.
- Execute the generated commands and display the output in the console.
- Supports both Windows and Linux operating systems. (Not yet tested on Linux :P)

## Prerequisites

- .NET 7.0 SDK
- LMStudio
- Gemma 2 2.5B model (available on Hugging Face)

## Setup

### Step 1: Clone the Repository

```sh
git clone https://github.com/ashsha19/iCMD.git
cd iCMD
```

### Step 2: Install .NET 8.0 SDK

Download and install the .NET 8.0 SDK from the official [.NET website](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).

### Step 3: Set Up LMStudio

1. Download and install LMStudio from the [official website](https://lmstudio.ai).
2. Download the Gemma 2 2.5B model from [Hugging Face](https://huggingface.co/google/gemma-2-2b-it-GGUF).
3. Search for google/gemma-2-2b-it-GGUF in LMStudio and pick a quantized model depending on your system's configuration.

### Step 4: Configure the Application

Update the Program.cs file to point to your local LMStudio server:

```csharp
HttpResponseMessage response = await client.PostAsync("http://localhost:1234/v1/chat/completions", content);
```

### Step 5: Build the Project

```sh
dotnet build
```

### Step 6: Run the Application

```sh
dotnet run
```

## Usage

1. Run the application using the command above.
2. Enter your natural language query when prompted.
3. The application will generate a PowerShell or Bash command based on your query.
4. You will be asked if you want to execute the generated command. Type `yes` to execute or `no` to cancel.
5. The output of the command will be displayed in the console.

## Example

```sh
Current Working Directory: D:\Experimental\C#\iCMD
Enter your natural language query (type 'quit' or 'exit' to terminate):
> show all files
Generating shell command...

Command Generated: Get-ChildItem -Path .

Do you want to execute this command? (yes/no)
> yes
```

## Note

This is an experimental project, and the output from the mini LLM may not always be accurate. Use it wisely and verify the generated commands before executing them.
