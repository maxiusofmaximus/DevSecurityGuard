using DevSecurityGuard.Service.Models;
using Microsoft.Extensions.Logging;

namespace DevSecurityGuard.Service.DetectionEngines;

/// <summary>
/// Detects typosquatting attacks by comparing package names to known popular packages
/// </summary>
public class TyposquattingDetector : IThreatDetector
{
    private readonly ILogger<TyposquattingDetector> _logger;
    private readonly HashSet<string> _popularPackages;

    public string DetectorName => "Typosquatting Detector";
    public int Priority => 90; // High priority

    public TyposquattingDetector(ILogger<TyposquattingDetector> logger)
    {
        _logger = logger;
        _popularPackages = LoadPopularPackages();
    }

    public async Task<ThreatDetectionResult> AnalyzePackageAsync(
        string packageName,
        string? version = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Analyzing package {PackageName} for typosquatting", packageName);

        // Check for exact matches in popular packages (not a threat)
        if (_popularPackages.Contains(packageName.ToLowerInvariant()))
        {
            return ThreatDetectionResult.NoThreat(packageName, version);
        }

        // Check for similar package names
        var similarPackage = FindSimilarPackage(packageName);
        if (similarPackage != null)
        {
            _logger.LogWarning("Potential typosquatting detected: {PackageName} is similar to {SimilarPackage}",
                packageName, similarPackage);

            return ThreatDetectionResult.CreateThreat(
                ThreatType.Typosquatting,
                ThreatSeverity.High,
                packageName,
                $"Package name is very similar to popular package '{similarPackage}'. This could be a typosquatting attack.",
                version);
        }

        return ThreatDetectionResult.NoThreat(packageName, version);
    }

    private string? FindSimilarPackage(string packageName)
    {
        var lowerPackageName = packageName.ToLowerInvariant();

        foreach (var popularPackage in _popularPackages)
        {
            // Calculate Levenshtein distance
            var distance = CalculateLevenshteinDistance(lowerPackageName, popularPackage);

            // If distance is 1-2, it's likely a typo
            if (distance > 0 && distance <= 2)
            {
                return popularPackage;
            }

            // Check for common character substitutions
            if (HasSuspiciousCharacterSubstitution(lowerPackageName, popularPackage))
            {
                return popularPackage;
            }
        }

        return null;
    }

    private int CalculateLevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source)) return target?.Length ?? 0;
        if (string.IsNullOrEmpty(target)) return source.Length;

        var distance = new int[source.Length + 1, target.Length + 1];

        for (int i = 0; i <= source.Length; i++)
            distance[i, 0] = i;

        for (int j = 0; j <= target.Length; j++)
            distance[0, j] = j;

        for (int i = 1; i <= source.Length; i++)
        {
            for (int j = 1; j <= target.Length; j++)
            {
                int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;

                distance[i, j] = Math.Min(
                    Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                    distance[i - 1, j - 1] + cost);
            }
        }

        return distance[source.Length, target.Length];
    }

    private bool HasSuspiciousCharacterSubstitution(string packageName, string popularPackage)
    {
        // Common substitutions: 0/O, 1/l/I, etc.
        var substitutions = new Dictionary<char, char[]>
        {
            { '0', new[] { 'o', 'O' } },
            { 'o', new[] { '0' } },
            { 'O', new[] { '0' } },
            { '1', new[] { 'l', 'I', 'i' } },
            { 'l', new[] { '1', 'I', 'i' } },
            { 'I', new[] { '1', 'l', 'i' } },
            { 'i', new[] { '1', 'l', 'I' } }
        };

        if (packageName.Length != popularPackage.Length)
            return false;

        int substitutionCount = 0;
        for (int i = 0; i < packageName.Length; i++)
        {
            if (packageName[i] != popularPackage[i])
            {
                if (substitutions.TryGetValue(packageName[i], out var possibleSubst))
                {
                    if (possibleSubst.Contains(popularPackage[i]))
                    {
                        substitutionCount++;
                    }
                    else
                    {
                        return false; // Different character that's not a known substitution
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        return substitutionCount > 0 && substitutionCount <= 2;
    }

    private HashSet<string> LoadPopularPackages()
    {
        // Top npm packages - in production, this should be loaded from a regularly updated database
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "react", "react-dom", "lodash", "express", "axios", "typescript", "webpack",
            "vue", "angular", "next", "jquery", "moment", "request", "chalk", "commander",
            "debug", "uuid", "prop-types", "classnames", "redux", "react-router-dom",
            "eslint", "prettier", "babel-core", "babel-loader", "webpack-cli", "dotenv",
            "cors", "body-parser", "mongoose", "nodemon", "jest", "mocha", "chai",
            "socket.io", "puppeteer", "cheerio", "yargs", "inquirer", "ora", "boxen",
            "eslint-config-airbnb", "node-fetch", "form-data", "ts-node", "rimraf",
            "cross-env", "concurrently", "husky", "lint-staged", "semantic-release",
            "@types/node", "@types/react", "@types/express", "glob", "fs-extra",
            "mkdirp", "minimist", "async", "bluebird", "underscore", "through2",
            "vinyl", "gulp", "grunt", "browserify", "rollup", "vite", "esbuild",
            "tailwindcss", "bootstrap", "material-ui", "@mui/material", "antd",
            "styled-components", "emotion", "sass", "less", "postcss", "autoprefixer",
            "graphql", "apollo-client", "prisma", "@prisma/client", "sequelize",
            "typeorm", "ioredis", "redis", "pg", "mysql", "sqlite3",
            "winston", "morgan", "pino", "bunyan", "log4js", "bcrypt", "bcryptjs",
            "jsonwebtoken", "passport", "helmet", "compression", "cookie-parser",
            "multer", "sharp", "jimp", "image-size", "pdf-lib", "pdfkit",
            "nodemailer", "mailgun-js", "sendgrid", "twilio", "stripe", "paypal",
            "aws-sdk", "@aws-sdk/client-s3", "firebase-admin", "@google-cloud/storage",
            "discord.js", "telegraf", "node-telegram-bot-api", "slack", "@slack/web-api",
            "ethers", "web3", "hardhat", "truffle", "@ethersproject/providers"
        };
    }
}
