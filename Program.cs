using System.Diagnostics;
using System.Text;
using Newtonsoft.Json;

namespace iCMD
{
  class Program
  {
    private static readonly string SystemMessage = "You are a senior system administrator. Your job is to generate windows powershell or Linux shell command based on user's query. Respond concisely only with the powershell or bash command in plain text with no formatting, no greetings, no thank you, no request, no references and no additional words.";
    private static readonly bool UseReflection = false;
    private static string OS = string.Empty;

    static async Task Main(string[] args)
    {
      OS = OperatingSystem.IsWindows() ? "windows" : "linux";

      while (true)
      {
        Console.WriteLine($"Current Working Directory: {Environment.CurrentDirectory}");
        Console.WriteLine("Enter your natural language query (type 'quit' or 'exit' to terminate):");

        string query = Console.ReadLine() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(query)) continue;

        if (query.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
            query.Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
          break;
        }

        try
        {
          await GenerateAndExecute(query);
        }
        catch (Exception ex)
        {
          Console.WriteLine($"An error occurred: {ex.Message}");
          Console.WriteLine("Please try again.");
        }
      }
    }

    static async Task GenerateAndExecute(string query, string? error = null, string? lastResponse = null)
    {
      Console.WriteLine($"Generating shell command...");
      string shellCommand = await GenerateShellCommandAsync(query, error, lastResponse);
      Console.WriteLine($"\nCommand Generated: {shellCommand}");

      Console.WriteLine("Do you want to execute this command? (yes/no)");
      string consent = Console.ReadLine() ?? string.Empty;

      if (consent.Equals("yes", StringComparison.OrdinalIgnoreCase))
      {
        error = await ExecuteShellCommandAsync(shellCommand);
        if (!string.IsNullOrEmpty(error))
        {
          Console.WriteLine("Do you want to try again? (yes/no)");
          consent = Console.ReadLine() ?? string.Empty;

          if (consent.Equals("yes", StringComparison.OrdinalIgnoreCase))
          {
            await GenerateAndExecute(query, error, shellCommand);
          }
        }
      }
      else
      {
        Console.WriteLine("Command execution cancelled.");
      }
    }

    static async Task<string> GenerateShellCommandAsync(string query, string? error = null, string? lastResponse = null)
    {
      var commandConsole = OS == "windows" ? "powershell" : "bash";
      var instruction = $"Generate {commandConsole} command for my {OS} operating system. My query is";
      var messagesWithExample = new[]
      {
          new { role = "system", content = SystemMessage },
          // new { role = "user", content = $"Generate {commandConsole} command for my {os} operating system." },
          // new { role = "assistant", content = "Ok" },
          new { role = "user", content = $"{instruction} ```show all files```" },
          new { role = "assistant", content = "Get-ChildItem -Path ." },
          new { role = "user", content = $"{instruction} ```what is the current directory```" },
          new { role = "assistant", content = "Get-Location" },
          new { role = "user", content = $"{instruction} ```{query}```" }
      }.ToList();

      if (!string.IsNullOrEmpty(error) && !string.IsNullOrEmpty(lastResponse))
      {
        messagesWithExample.Add(new { role = "assistant", content = lastResponse });
        messagesWithExample.Add(new { role = "user", content = $"Verify and generate the command again. I am getting this error after executing the command you shared: ```{error}```\n\n\nOutput just the command." });
      }

      using (HttpClient client = new HttpClient())
      {
        var requestBody = new
        {
          // model = ModelName,
          messages = messagesWithExample,
          temperature = 0,
          max_tokens = 100,
          stream = false
        };

        string jsonRequestBody = JsonConvert.SerializeObject(requestBody);
        StringContent content = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await client.PostAsync("http://localhost:1234/v1/chat/completions", content);
        string jsonResponseBody = await response.Content.ReadAsStringAsync();

        dynamic responseObject = !string.IsNullOrEmpty(jsonResponseBody) ? JsonConvert.DeserializeObject(jsonResponseBody) : null;
        string shellCommand = responseObject?.choices[0]?.message?.content;

        if (string.IsNullOrWhiteSpace(shellCommand)) throw new Exception("No output generated.");

        // removing <|end_of_text|><|begin_of_text|> from output
        shellCommand = shellCommand.Replace("<|end_of_text|>", "\n").Replace("<|begin_of_text|>", "\n");

        // using reflection
        if (UseReflection)
        {
          messagesWithExample.Add(new { role = "assistant", content = shellCommand });
          messagesWithExample.Add(new { role = "user", content = $"Are you sure this is correct? Output the command only." });

          requestBody = new
          {
            // model = ModelName,
            messages = messagesWithExample,
            temperature = 0,
            max_tokens = 100,
            stream = false
          };

          jsonRequestBody = JsonConvert.SerializeObject(requestBody);
          content = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");

          response = await client.PostAsync("http://localhost:1234/v1/chat/completions", content);
          jsonResponseBody = await response.Content.ReadAsStringAsync();

          responseObject = !string.IsNullOrEmpty(jsonResponseBody) ? JsonConvert.DeserializeObject(jsonResponseBody) : null;
          shellCommand = responseObject?.choices[0]?.message?.content;

          if (string.IsNullOrWhiteSpace(shellCommand)) throw new Exception("No output generated.");

          // removing <|end_of_text|><|begin_of_text|> from output
          shellCommand = shellCommand.Replace("<|end_of_text|>", "\n").Replace("<|begin_of_text|>", "\n");
        }

        return shellCommand;
      }
    }

    static async Task<string> ExecuteShellCommandAsync(string command)
    {
      Process process = new Process();
      ProcessStartInfo startInfo = new ProcessStartInfo();

      // // escape double qoute
      // command = command.Replace("\"", "\\\"");

      // Check if the current operating system is Windows or Linux
      if (OS == "windows")
      {
        var commandInBytes = Encoding.Unicode.GetBytes(command);
        var encodedCommand = Convert.ToBase64String(commandInBytes);
        startInfo.FileName = "powershell.exe";
        startInfo.Arguments = $"-encodedCommand {encodedCommand}";
      }
      else
      {
        startInfo.FileName = "/bin/bash";
        startInfo.Arguments = $"-c \"{command}\"";
      }

      startInfo.RedirectStandardOutput = true;
      startInfo.RedirectStandardError = true;
      startInfo.UseShellExecute = false;
      startInfo.CreateNoWindow = true;

      process.StartInfo = startInfo;

      var executionOutput = string.Empty;
      process.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
      {
        if (!string.IsNullOrEmpty(e.Data))
        {
          executionOutput += e.Data;
          Console.WriteLine(e.Data);
        }
      });

      var executionError = string.Empty;
      process.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
      {
        if (!string.IsNullOrEmpty(e.Data))
        {
          if (string.IsNullOrEmpty(executionError)) { executionError = "Error:"; }

          executionError += Environment.NewLine + e.Data;
        }
      });

      process.Start();
      process.BeginOutputReadLine();
      process.BeginErrorReadLine();
      await process.WaitForExitAsync();

      if (!string.IsNullOrEmpty(executionError) && string.IsNullOrEmpty(executionOutput))
      {
        Console.WriteLine($"Error: {executionError}");
        return executionError;
      }

      return string.Empty;
    }
  }
}
