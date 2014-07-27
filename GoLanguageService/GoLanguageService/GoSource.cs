using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitbos.GoLanguageService
{
	public class GoSource : Source
	{
		public GoSource( LanguageService service, IVsTextLines textLines, Colorizer colorizer )
			: base( service, textLines, colorizer )
		{
		}

		public override CommentInfo GetCommentFormat()
		{
			var info = new CommentInfo();
			info.LineStart = "//";
			info.BlockStart = "/*";
			info.BlockEnd = "*/";
			info.UseLineComments = false;
			return info;
		}

		public override void OnCommand( IVsTextView textView, VSConstants.VSStd2KCmdID command, char ch )
		{
			base.OnCommand( textView, command, ch );
		}

		public override void MatchBraces( IVsTextView textView, int line, int index, TokenInfo info )
		{
			base.MatchBraces( textView, line, index, info );
		}
	}
}
