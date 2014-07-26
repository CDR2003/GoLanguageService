using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService
{
	public class GoScanner : IScanner
	{
		private List<GoToken> m_tokens;

		private int m_currentIndex;

		private Process m_scannerApp;

		public GoScanner()
		{
			m_tokens = new List<GoToken>();
			m_currentIndex = 0;

			m_scannerApp = new Process();
			m_scannerApp.StartInfo.FileName = GoHelper.ScannerPath;
			m_scannerApp.StartInfo.CreateNoWindow = true;
			m_scannerApp.StartInfo.RedirectStandardInput = true;
			m_scannerApp.StartInfo.RedirectStandardOutput = true;
			m_scannerApp.StartInfo.UseShellExecute = false;
		}

		public bool ScanTokenAndProvideInfoAboutIt( TokenInfo tokenInfo, ref int state )
		{
			if( m_currentIndex >= m_tokens.Count )
			{
				return false;
			}

			var token = m_tokens[m_currentIndex];
			tokenInfo.Type = token.Type;
			tokenInfo.Color = GetColor( tokenInfo.Type );
			tokenInfo.StartIndex = token.StartIndex;
			tokenInfo.EndIndex = tokenInfo.StartIndex + token.Text.Length - 1;
			tokenInfo.Token = token.ID;
			tokenInfo.Trigger = GetTriggers( tokenInfo.Token );

			m_currentIndex++;
			return true;
		}

		public void SetSource( string source, int offset )
		{
			m_scannerApp.Start();
			m_scannerApp.StandardInput.Write( source.Substring( offset ) );
			m_scannerApp.StandardInput.Close();

			var json = m_scannerApp.StandardOutput.ReadToEnd();
			m_scannerApp.WaitForExit();

			m_tokens = JsonConvert.DeserializeObject<List<GoToken>>( json );
			m_currentIndex = 0;
		}

		private static TokenColor GetColor( TokenType type )
		{
			switch( type )
			{
				case TokenType.Comment:
				case TokenType.LineComment:
					return TokenColor.Comment;
				case TokenType.Identifier:
					return TokenColor.Identifier;
				case TokenType.Keyword:
					return TokenColor.Keyword;
				case TokenType.Literal:
					return TokenColor.Number;
				case TokenType.Delimiter:
				case TokenType.Operator:
				case TokenType.Text:
				case TokenType.Unknown:
				case TokenType.WhiteSpace:
					return TokenColor.Text;
				case TokenType.String:
					return TokenColor.String;
			}

			return TokenColor.Text;
		}

		private static TokenTriggers GetTriggers( int tokenID )
		{
			return TokenTriggers.None;
		}
	}
}
