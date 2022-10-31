using System.Text;

namespace libNOM.collect.Extensions;


internal static class StringBuilderExtensions
{
    internal static string ToAlphaNumericString(this StringBuilder self)
    {
        return self.ToString().ToAlphaNumericString();
    }
}
