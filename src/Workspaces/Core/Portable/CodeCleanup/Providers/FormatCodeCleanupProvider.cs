﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeCleanup.Providers
{
    internal class FormatCodeCleanupProvider : ICodeCleanupProvider
    {
        public string Name
        {
            get { return PredefinedCodeCleanupProviderNames.Format; }
        }

        public async Task<Document> CleanupAsync(Document document, IEnumerable<TextSpan> spans, CancellationToken cancellationToken)
        {
            // If the old text already exists, use the fast path for formatting.
            SourceText oldText;
            if (document.TryGetText(out oldText))
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var textChanges = Formatter.GetFormattedTextChanges(root, spans, document.Project.Solution.Workspace, cancellationToken: cancellationToken);
                if (textChanges.Count == 0)
                {
                    return document;
                }

                var newText = oldText.WithChanges(textChanges);
                return document.WithText(newText);
            }

            return await Formatter.FormatAsync(document, spans, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public SyntaxNode Cleanup(SyntaxNode root, IEnumerable<TextSpan> spans, Workspace workspace, CancellationToken cancellationToken)
        {
            // If the old text already exists, use the fast path for formatting.
            SourceText oldText;
            if (root.SyntaxTree != null && root.SyntaxTree.TryGetText(out oldText))
            {
                var changes = Formatter.GetFormattedTextChanges(root, spans, workspace, cancellationToken: cancellationToken);

                if (changes.Count == 0)
                {
                    return root;
                }

                return root.SyntaxTree.WithChangedText(oldText.WithChanges(changes)).GetRoot(cancellationToken);
            }

            return Formatter.Format(root, spans, workspace, cancellationToken: cancellationToken);
        }
    }
}
