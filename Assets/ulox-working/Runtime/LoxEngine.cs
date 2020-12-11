﻿using System;
using System.Collections.Generic;

namespace ULox
{
    public class LoxEngine
    {
        private Scanner _scanner;
        private Parser _parser;
        private Interpreter _interpreter;
        private Action<string> _logger;
        
        public LoxEngine(
            Scanner scanner, 
            Parser parser,
            Interpreter interpreter, 
            Action<string> logger)
        {
            _scanner = scanner;
            _parser = parser;
            _interpreter = interpreter;
            _logger = logger;
        }

        public void Run(string text)
        {
            _scanner.Reset();
            _scanner.Scan(text);
            _parser.Reset();
            var statements = _parser.Parse(_scanner.Tokens);

            //// Stop if there was a syntax error.
            //if (expression == null) return;

            ////_logger(new ASTPrinter().Print(expression));
            _interpreter.Interpret(statements);
        }
    }
}