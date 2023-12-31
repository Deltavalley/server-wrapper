﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerWrapper
{
	public static class ConsoleHelper
	{
		/// <summary>
		/// Verifies a console input matches the required type
		/// <para><paramref name="prompt"/>: the prompt without a space at the end.</para>
		/// </summary>
		/// <typeparam name="T">The type of the input to return</typeparam>
		/// <returns></returns>
		public static T VerifyConsoleInput<T>(string prompt, Predicate<string> isValid, Converter<string,T> converter, bool isOptional = false)
		{
			prompt += " ";
			//add null check to the predicate in
			Predicate<string> isValidNotEmpty = (string y) => { return (!string.IsNullOrEmpty(y) && isValid(y)) || (isOptional && string.IsNullOrEmpty(y)); };

			//write the prompt
			Console.Write($"{prompt}");
			//get the reponse
			string input = GetRawInput();

			bool valid = isValidNotEmpty(input);
			while (!valid)
			{
				ClearLine();
				Console.Write($"\r{prompt}");

				//get the input
				input = GetRawInput();

				//validate
				valid = isValidNotEmpty(input);
			}
			Console.WriteLine();
			return converter(input!);
		}

		static string GetRawInput()
		{
			StringBuilder input = new();
			while (true)
			{
				var key = Console.ReadKey(true);
				if (key.Key == ConsoleKey.Enter)
				{
					break;
				}
				else if (key.Key == ConsoleKey.Backspace && input.Length > 0)
				{
					input.Remove(input.Length - 1, 1);
					Console.Write("\b \b");
				}
				else if (key.Key != ConsoleKey.Backspace)
				{
					Console.Write(key.KeyChar);
					input.Append(key.KeyChar);
				}
			}
			return input.ToString();
		}

		static void ClearLine()
		{
			Console.Write("\r" + new string(' ', Console.WindowWidth) + "\r");
		}

		public static string VerifyConsoleInput(string prompt, Predicate<string> isValid, bool isOptional = false)
		{
			Converter<string, string> dummyConverter = (string x) => x;
			return VerifyConsoleInput(prompt,isValid,dummyConverter, isOptional);
		}
	}
}
