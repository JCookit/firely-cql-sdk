﻿using Ncqa.Elm.Expressions;

namespace Ncqa.Elm.Expressions
{
    public class CodeSystemDefinitionExpression: Expression
    {
        public string? name { get; set; }

        public string? id { get; set; }
    }
}