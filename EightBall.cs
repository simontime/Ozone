namespace Ozone
{
    class EightBall
    {
        // Thanks to the unknown writer behind these quotes.

        public static readonly string[] Replies =
        {
            "Signs point to yes. Except that you were born an idiot, you will die an idiot, and nothing will change in-between.",
            "Without a doubt. Nah, I’m just messing with you, go fuck yourself.",
            "My sources say no. They also tell me they hate you and hope you burn in hell.",
            "Yes, definitely. Unless it doesn’t happen. Listen it’s not my fault your father didn’t love you. Get off my back!",
            "Outlook not so good. Especially since you’re so goddamn ugly.",
            "All signs point to yes. But on second thought, go fuck yourself.",
            "As if!",
            "Ask me if I care!",
            "Dumb question. Ask another.",
            "Forget about it.",
            "Get a clue.",
            "In your dreams.",
            "Not a chance.",
            "Obviously.",
            "Oh, please.",
            "Sure.",
            "That's ridiculous.",
            "Well... maybe?",
            "What do *you* think?",
            "Yes... you prick.",
            "Who cares?",
            "Yeah, and I'm the fucking pope.",
            "Yeah, right.",
            "You wish.",
            "You've got to be taking the piss...",
            "No u."
        };

        public static string GetRandomMessage()
        {
            return Replies.RandomElement();
        }
    }
}
