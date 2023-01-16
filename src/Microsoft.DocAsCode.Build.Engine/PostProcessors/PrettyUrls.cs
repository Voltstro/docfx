#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HtmlAgilityPack;
using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.Build.Engine;

public class PrettyUrls : HtmlDocumentHandler
{
    protected override void HandleCore(HtmlDocument document, ManifestItem manifestItem, string inputFile,
        string outputFile)
    {
        string addToFront = string.Empty;
        if (manifestItem.OutputFiles[".html"].RelativePath != "index.html")
            addToFront = "../";

        var hyperLinkNodes = document.DocumentNode.SelectNodes($"//a");
        if (hyperLinkNodes != null)
        {
            foreach (HtmlNode node in hyperLinkNodes)
            {
                var href = node.Attributes
                    .FirstOrDefault(x => x.Name.Equals("href", StringComparison.InvariantCultureIgnoreCase));
                if(href == null)
                    continue;

                if(href.Value.EndsWith(".html"))
                    href.Value = $"{addToFront}{UpdatePath(href.Value, false)}/";
            }
        }

        var linkNodes = document.DocumentNode.SelectNodes($"//link");
        if (linkNodes != null)
        {
            foreach (HtmlNode linkNode in linkNodes)
            {
                var href = linkNode.Attributes.FirstOrDefault(x =>
                    x.Name.Equals("href", StringComparison.InvariantCultureIgnoreCase));
                if(href == null)
                    continue;

                href.Value = Path.Combine(addToFront, href.Value);
            }
        }

        var scriptNodes = document.DocumentNode.SelectNodes($"//script|//img");
        if (scriptNodes != null)
        {
            foreach (HtmlNode scriptNode in scriptNodes)
            {
                var src = scriptNode.Attributes.FirstOrDefault(x =>
                    x.Name.Equals("src", StringComparison.InvariantCultureIgnoreCase));
                if(src == null)
                    continue;

                src.Value = Path.Combine(addToFront, src.Value);
            }
        }
    }

    protected override Manifest PreHandleCore(Manifest manifest)
    {
        foreach (ManifestItem manifestFile in manifest.Files)
        {
            foreach (KeyValuePair<string,OutputFileInfo> outputFile in manifestFile.OutputFiles)
            {
                if (outputFile.Key.Equals(".html"))
                {
                    Logger.LogVerbose($"Updating {outputFile.Value.RelativePath}...");

                    //This is the main index.html
                    if(outputFile.Value.RelativePath == "index.html" || outputFile.Value.RelativePath.EndsWith("toc.html"))
                        continue;

                    outputFile.Value.RelativePath = UpdatePath(outputFile.Value.RelativePath);
                }
            }
        }

        return manifest;
    }

    private static string UpdatePath(string path, bool includeIndexFileName = true)
    {
        if (!includeIndexFileName && path == "index.html")
            return "/";

        string? baseDir = Path.GetDirectoryName(path);

        //Get just the file name
        string? fileName = Path.GetFileNameWithoutExtension(path);

        return Path.Combine(baseDir, fileName, includeIndexFileName ? "index.html" : string.Empty);
    }
}
