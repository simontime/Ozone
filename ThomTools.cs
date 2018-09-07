using System;
using System.Collections.Generic;

namespace Ozone
{
    class ThomTools
    {
        public static string GeneratePhrase()
        {
            string[] First =
            {
                "can ",
                "can to ",
                "you can ",
                "any here ",
                "any here can ",
                "any here can to ",
                "can you ",
                "can with ",
                "can we ",
                "are to ",
                "are you ",
                "hi ",
                "hello ",
                "hello can ",
                "you know ",
                "any that ",
                "you know any that can "

            };
            string[] Second =
            {
                "help ",
                "help to ",
                "pls help with ",
                "please help with ",
                "please help to ",
                "respond to me ",
                "respond to me about ",
                "respond about ",
                "how to ",
                "how to do ",
                "reply me pls ",
                "reply pls about ",
                "help me with ",
                "tell me how ",
                "tell how ",
                "explain how ",
                "give method ",
                "tell method ",
                "tell to me ",
                "reply me ",
                "reply me method ",
                "reply me how ",
                "reply me help "
            };
            string[] Third =
            {
                "build atmosphere ",
                "make atmosphere ",
                "build amostrephe ",
                "make amostrephe ",
                "build atmos ",
                "make atmos ",
                "hactool ",
                "version bot ",
                "versionlist ",
                "setup bot ",
                "put sx os ",
                "webhook ",
                "ryujinx ",
                "yuzu ",
                "make ryujinx ",
                "make yuzu ",
                "sx os ",
                "download rom ",
                "play back up game ",
                "fuse gelle ",
                "fake gazelle "
            };
            string[] Fourth =
            {
                "please ",
                "i dont understand ",
                "then do ",
                "thx ",
                "pls ",
                "then "
            };
            string[] Last =
            {
                "?",
                "!",
                "...",
                "! !",
                "."
            };
            return First.RandomElement() + Second.RandomElement() + Third.RandomElement() + Fourth.RandomElement() + Last.RandomElement();
        }
    }
}
