using System;

namespace wotb_scanner
{

    using Tesseract;
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.Text;
    using System.Drawing;
    class Program
    {
        public static void Main(string[] args)
        {
            var testImagePath = @"C:\00\code\wotbplayerscanner\working.file.png";
            if (args.Length > 0)
            {
                testImagePath = args[0];
            }

            try
            {
                           var logger = new FormattedConsoleLogger();
                var resultPrinter = new ResultPrinter(logger);

                using (var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default))
                {
                    using (var img = Pix.LoadFromFile(testImagePath))
                    {
                        using (logger.Begin("Process image"))
                        {
                            var i = 1;
                            using (var page = engine.Process(img))
                            {
                                var text = page.GetText();
                                logger.Log("Text: {0}", text);
                                logger.Log("Mean confidence: {0}", page.GetMeanConfidence());

                                using (var iter = page.GetIterator())
                                {
                                    iter.Begin();
                                    do
                                    {
                                        if (i % 2 == 0)
                                        {
                                            using (logger.Begin("Line {0}", i))
                                            {
                                                do
                                                {
                                                    using (logger.Begin("Word Iteration"))
                                                    {
                                                        if (iter.IsAtBeginningOf(PageIteratorLevel.Block))
                                                        {
                                                            logger.Log("New block");
                                                        }
                                                        if (iter.IsAtBeginningOf(PageIteratorLevel.Para))
                                                        {
                                                            logger.Log("New paragraph");
                                                        }
                                                        if (iter.IsAtBeginningOf(PageIteratorLevel.TextLine))
                                                        {
                                                            logger.Log("New line");
                                                        }
                                                        logger.Log("word: " + iter.GetText(PageIteratorLevel.Word));
                                                    }
                                                } while (iter.Next(PageIteratorLevel.TextLine, PageIteratorLevel.Word));
                                            }
                                        }
                                        i++;
                                    } while (iter.Next(PageIteratorLevel.Para, PageIteratorLevel.TextLine));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                Console.WriteLine("Unexpected Error: " + e.Message);
                Console.WriteLine("Details: ");
                Console.WriteLine(e.ToString());
            }
            Console.Write("Press any key to continue . . . ");
            Console.ReadKey(true);
        }



        public class ResultPrinter
        {
            readonly FormattedConsoleLogger logger;

            public ResultPrinter(FormattedConsoleLogger logger)
            {
                this.logger = logger;
            }

            public void Print(ResultIterator iter)
            {
                logger.Log("Is beginning of block: {0}", iter.IsAtBeginningOf(PageIteratorLevel.Block));
                logger.Log("Is beginning of para: {0}", iter.IsAtBeginningOf(PageIteratorLevel.Para));
                logger.Log("Is beginning of text line: {0}", iter.IsAtBeginningOf(PageIteratorLevel.TextLine));
                logger.Log("Is beginning of word: {0}", iter.IsAtBeginningOf(PageIteratorLevel.Word));
                logger.Log("Is beginning of symbol: {0}", iter.IsAtBeginningOf(PageIteratorLevel.Symbol));

                logger.Log("Block text: \"{0}\"", iter.GetText(PageIteratorLevel.Block));
                logger.Log("Para text: \"{0}\"", iter.GetText(PageIteratorLevel.Para));
                logger.Log("TextLine text: \"{0}\"", iter.GetText(PageIteratorLevel.TextLine));
                logger.Log("Word text: \"{0}\"", iter.GetText(PageIteratorLevel.Word));
                logger.Log("Symbol text: \"{0}\"", iter.GetText(PageIteratorLevel.Symbol));
            }
        }

        public class FormattedConsoleLogger
        {
            const string Tab = "    ";
            private class Scope : DisposableBase
            {
                private int indentLevel;
                private string indent;
                private FormattedConsoleLogger container;

                public Scope(FormattedConsoleLogger container, int indentLevel)
                {
                    this.container = container;
                    this.indentLevel = indentLevel;
                    StringBuilder indent = new StringBuilder();
                    for (int i = 0; i < indentLevel; i++)
                    {
                        indent.Append(Tab);
                    }
                    this.indent = indent.ToString();
                }

                public void Log(string format, object[] args)
                {
                    var message = String.Format(format, args);
                    StringBuilder indentedMessage = new StringBuilder(message.Length + indent.Length * 10);
                    int i = 0;
                    bool isNewLine = true;
                    while (i < message.Length)
                    {
                        if (message.Length > i && message[i] == '\r' && message[i + 1] == '\n')
                        {
                            indentedMessage.AppendLine();
                            isNewLine = true;
                            i += 2;
                        }
                        else if (message[i] == '\r' || message[i] == '\n')
                        {
                            indentedMessage.AppendLine();
                            isNewLine = true;
                            i++;
                        }
                        else
                        {
                            if (isNewLine)
                            {
                                indentedMessage.Append(indent);
                                isNewLine = false;
                            }
                            indentedMessage.Append(message[i]);
                            i++;
                        }
                    }

                    Console.WriteLine(indentedMessage.ToString());

                }

                public Scope Begin()
                {
                    return new Scope(container, indentLevel + 1);
                }

                protected override void Dispose(bool disposing)
                {
                    if (disposing)
                    {
                        var scope = container.scopes.Pop();
                        if (scope != this)
                        {
                            throw new InvalidOperationException("Format scope removed out of order.");
                        }
                    }
                }
            }

            private Stack<Scope> scopes = new Stack<Scope>();

            public IDisposable Begin(string title = "", params object[] args)
            {
                Log(title, args);
                Scope scope;
                if (scopes.Count == 0)
                {
                    scope = new Scope(this, 1);
                }
                else
                {
                    scope = ActiveScope.Begin();
                }
                scopes.Push(scope);
                return scope;
            }

            public void Log(string format, params object[] args)
            {
                if (scopes.Count > 0)
                {
                    ActiveScope.Log(format, args);
                }
                else
                {
                    Console.WriteLine(String.Format(format, args));
                }
            }

            private Scope ActiveScope
            {
                get
                {
                    var top = scopes.Peek();
                    if (top == null) throw new InvalidOperationException("No current scope");
                    return top;
                }
            }
        }
    }

}

