using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ULox;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    //TODO more tests

    /*
    if(a < b or rand >= PI) c = rand;
    */
    public class ScannerTests
    {

        private static string[] StringDeclareTestStrings = new string[]
        {
             @"var lang = ""lox"";",
             @"var lang=""lox"";",
             @"var lang
=
""lox""
;",
            @"var multi = ""Now is the winter of our discontent
Made glorious summer by this sun of York;
And all the clouds that lour'd upon our house
In the deep bosom of the ocean buried.""; ",
    };

        [Test]
        [TestCaseSource(nameof(StringDeclareTestStrings))]
        public void Scanner_StringVarDeclare_TokenTypeMatch(string testString)
        {
            var tokenResults = new TokenType[]
            {
                TokenType.VAR,
                TokenType.IDENT,
                TokenType.ASSIGN,
                TokenType.STRING,
                TokenType.END_STATEMENT,
                TokenType.EOF
            };

            var scanner = new Scanner(null);

            scanner.Scan(testString);

            var resultingTokenTypes = scanner.Tokens.Select(x => x.TokenType).ToArray();

            for (int i = 0; i < resultingTokenTypes.Length; i++)
            {
                Assert.AreEqual(tokenResults[i], resultingTokenTypes[i]);
            }
        }

        private static string[] IntDeclareTestStrings = new string[]
        {
             @"var rand = 7;",
             @"var rand=7;",
             @"var rand =7;",
             @"var rand= 7;",
             @"var rand= 7 ;",
             @"var   rand    =   7   ;",
             @"var   
rand    
=   
7   
;",
             @"var rand = 71234 ;",
        };

        [Test]
        [TestCaseSource(nameof(IntDeclareTestStrings))]
        public void Scanner_IntVarDeclare_TokenTypeMatch(string testString)
        {
            var tokenResults = new TokenType[]
            {
                TokenType.VAR,
                TokenType.IDENT,
                TokenType.ASSIGN,
                TokenType.INT,
                TokenType.END_STATEMENT,
                TokenType.EOF
            };

            var scanner = new Scanner(null);

            scanner.Scan(testString);

            var resultingTokenTypes = scanner.Tokens.Select(x => x.TokenType).ToArray();

            for (int i = 0; i < resultingTokenTypes.Length; i++)
            {
                Assert.AreEqual(tokenResults[i], resultingTokenTypes[i]);
            }
        }

        private static string[] FloatDeclareTestStrings = new string[]
        {
             @"var PI = 3.14;",
             @"var PI= 3.14;",
             @"var PI =3.14;",
             @"var PI=3.14;",
             @"var
PI
=
3.14
;",
             @"var PI = 3.;",
        };

        [Test]
        [TestCaseSource(nameof(FloatDeclareTestStrings))]
        public void Scanner_FloatVarDeclare_TokenTypeMatch(string testString)
        {
            var tokenResults = new TokenType[]
            {
                TokenType.VAR,
                TokenType.IDENT,
                TokenType.ASSIGN,
                TokenType.FLOAT,
                TokenType.END_STATEMENT,
                TokenType.EOF
            };

            var scanner = new Scanner(null);

            scanner.Scan(testString);

            var resultingTokenTypes = scanner.Tokens.Select(x => x.TokenType).ToArray();

            for (int i = 0; i < resultingTokenTypes.Length; i++)
            {
                Assert.AreEqual(tokenResults[i], resultingTokenTypes[i]);
            }
        }
    }
}
