using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using BoneLib.BoneMenu;
using BoneLib.Notifications;

using MelonLoader;

using Semver;

using UnityEngine;

namespace ItemReplacer.Utilities
{
    public class Thunderstore
    {
        public readonly string UserAgent;
        public bool IsV1Deprecated { get; set; }

        private Package _fetchedPackage;

        private bool _isLatestVersion;

        private string _currentVersion;

        public Thunderstore(string userAgent)
        {
            UserAgent = userAgent;
        }

        public Thunderstore(string userAgent, bool isV1Deprecated) : this(userAgent)
        {
            IsV1Deprecated = isV1Deprecated;
        }

        public Thunderstore()
        {
            var executing = System.Reflection.Assembly.GetExecutingAssembly();
            if (executing != null)
            {
                var name = executing.GetName();
                if (name != null)
                {
                    UserAgent = $"{name.Name} / {name.Version} C# Application";
                }
            }
        }

        public Thunderstore(bool isV1Deprecated)
        {
            this.IsV1Deprecated = isV1Deprecated;
            var executing = System.Reflection.Assembly.GetExecutingAssembly();
            if (executing != null)
            {
                var name = executing.GetName();
                if (name != null)
                {
                    UserAgent = $"{name.Name} / {name.Version}";
                }
            }
        }

        public void BL_FetchPackage(string name, string author, string currentVersion, MelonLogger.Instance logger = null)
        {
            if (_fetchedPackage != null)
                return;

            _currentVersion = currentVersion;

            try
            {
                _fetchedPackage = GetPackage(author, name);
                if (_fetchedPackage == null)
                    logger?.Warning($"Could not find Thunderstore package for {name}");

                if (string.IsNullOrWhiteSpace(_fetchedPackage.Latest?.Version))
                    logger?.Warning("Latest version could not be found or the version is empty");

                _isLatestVersion = _fetchedPackage.IsLatestVersion(currentVersion);
                if (!_isLatestVersion)
                    logger?.Warning($"A new version of {name} is available: v{_fetchedPackage.Latest.Version} while the current is v{currentVersion}. It is recommended that you update");
                else if (SemVersion.Parse(currentVersion) == _fetchedPackage.Latest.SemanticVersion)
                    logger?.Msg($"Latest version of {name} is installed! (v{currentVersion})");
                else
                    logger?.Msg($"Beta release of {name} is installed (v{_fetchedPackage.Latest.Version} is newest, v{currentVersion} is installed)");
            }
            catch (ThunderstorePackageNotFoundException)
            {
                logger?.Warning($"Could not find Thunderstore package for {name}");
            }
            catch (Exception e)
            {
                logger?.Error($"An unexpected error has occurred while trying to check if {name} is the latest version", e);
            }
        }

        /// <summary>
        /// This requires <see cref="BL_FetchPackage"/> to be called first."/>
        /// </summary>
        public void BL_CreateMenuLabel(Page page, bool createBlankSpace = true)
        {
            if (_fetchedPackage == null)
                return;

            if (createBlankSpace)
                page.CreateFunction("", Color.white, null).SetProperty(ElementProperties.NoBorder);

            const string green = "#00FF00";

            page.CreateFunction(
                $"Current Version: v{_currentVersion}" +
                $"{(_isLatestVersion || _fetchedPackage == null ? string.Empty : $"<br><color={green}>(Update available!)</color>")}", Color.white,
                null).SetProperty(ElementProperties.NoBorder);
        }

        /// <summary>
        /// This requires <see cref="BL_FetchPackage"/> to be called first."/>
        /// </summary>
        public void BL_SendNotification()
        {
            if (_fetchedPackage == null || _isLatestVersion)
                return;

            var text = new NotificationText($"There is a new version of {_fetchedPackage.Name}. " +
                $"Go to Thunderstore and download the latest version which is <color=#00FF00>v{_fetchedPackage.Latest.Version}</color>", Color.white, true);
            Notifier.Send(new Notification()
            {
                Title = "Update!",
                Message = text,
                PopupLength = 5f,
                ShowTitleOnPopup = true,
                Type = NotificationType.Warning,
            });
        }

        public Package GetPackage(string @namespace, string name, string version = null)
        {
            var res = SendRequest<Package>($"https://thunderstore.io/api/experimental/package/{@namespace}/{name}/{version ?? string.Empty}");
            if (!IsV1Deprecated && res != null)
            {
                var metrics = GetPackageMetrics(@namespace, name);
                if (metrics != null)
                {
                    res.TotalDownloads = metrics.Downloads;
                    res.RatingScore = metrics.RatingScore;
                }
            }
            return res;
        }

        private static bool IsTaskGood<T>(Task<T> task) where T : class
            => task?.IsCompletedSuccessfully == true && task.Result != null;

        public T SendRequest<T>(string url)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", this.UserAgent);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            var request = client.GetAsync(url);
            request.Wait();
            var result = request?.Result;
            if (IsTaskGood(request))
            {
                if (!result.IsSuccessStatusCode)
                {
                    HandleHttpError(result);
                    return default;
                }

                var content = result.Content.ReadAsStringAsync();
                content.Wait();
                var result2 = content?.Result;
                if (IsTaskGood(content))
                    return JsonSerializer.Deserialize<T>(result2);
            }
            return default;
        }

        private static void HandleHttpError(HttpResponseMessage result)
        {
            if (IsThunderstoreError(result))
            {
                if (IsPackageNotFound(result))
                    throw new ThunderstorePackageNotFoundException("Thunderstore could not find the package");
                else
                    throw new ThunderstoreErrorException("Thunderstore API has thrown an unexpected error!", result);
            }
            else
            {
                result.EnsureSuccessStatusCode();
            }
        }

        public V1PackageMetrics GetPackageMetrics(string @namespace, string name)
        {
            if (IsV1Deprecated)
                return null;

            return SendRequest<V1PackageMetrics>($"https://thunderstore.io/api/v1/package-metrics/{@namespace}/{name}/");
        }

        public bool IsLatestVersion(string @namespace, string name, string currentVersion)
        {
            if (SemVersion.TryParse(currentVersion, out var version))
            {
                return IsLatestVersion(@namespace, name, version);
            }
            return false;
        }

        public bool IsLatestVersion(string @namespace, string name, Version currentVersion)
        {
            return IsLatestVersion(@namespace, name, new SemVersion(currentVersion));
        }

        public bool IsLatestVersion(string @namespace, string name, SemVersion currentVersion)
        {
            if (!IsV1Deprecated)
            {
                var package = GetPackageMetrics(@namespace, name);
                if (package == null) return false;
                return package.IsLatestVersion(currentVersion);
            }
            else
            {
                var package = GetPackage(@namespace, name);
                if (package == null) return false;
                return package.IsLatestVersion(currentVersion);
            }
        }

        private static bool IsPackageNotFound(HttpResponseMessage response)
        {
            const string detect = "Not found.";
            if (response.StatusCode != HttpStatusCode.NotFound)
            {
                return false;
            }
            else
            {
                var @string = response.Content.ReadAsStringAsync();
                @string.Wait();
                var _string = @string.Result;
                if (string.IsNullOrWhiteSpace(_string))
                {
                    return false;
                }
                else
                {
                    ThunderstoreErrorResponse error;
                    try
                    {
                        error = JsonSerializer.Deserialize<ThunderstoreErrorResponse>(_string);
                    }
                    catch (JsonException)
                    {
                        return false;
                    }
                    if (error != null)
                    {
                        return string.Equals(error.Details, detect, StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            return false;
        }

        private static bool IsThunderstoreError(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return false;
            }
            else
            {
                var @string = response.Content.ReadAsStringAsync();
                @string.Wait();
                var _string = @string.Result;
                if (string.IsNullOrWhiteSpace(_string))
                {
                    return false;
                }
                else
                {
                    ThunderstoreErrorResponse error;
                    try
                    {
                        error = JsonSerializer.Deserialize<ThunderstoreErrorResponse>(_string);
                    }
                    catch (JsonException)
                    {
                        return false;
                    }
                    if (error != null)
                    {
                        return !string.IsNullOrWhiteSpace(error.Details);
                    }
                }
            }
            return false;
        }
    }

    public class Package
    {
        [JsonPropertyName("namespace")]
        [JsonInclude]
        public string Namespace { get; internal set; }

        [JsonPropertyName("name")]
        [JsonInclude]
        public string Name { get; internal set; }

        [JsonPropertyName("full_name")]
        [JsonInclude]
        public string FullName { get; internal set; }

        [JsonPropertyName("owner")]
        [JsonInclude]
        public string Owner { get; internal set; }

        [JsonPropertyName("package_url")]
        [JsonInclude]
        public string PackageUrl { get; internal set; }

        [JsonPropertyName("date_created")]
        [JsonInclude]
        public DateTime CreatedAt { get; internal set; }

        [JsonPropertyName("date_updated")]
        [JsonInclude]
        public DateTime UpdatedAt { get; internal set; }

        [JsonPropertyName("rating_score")]
        [JsonInclude]
        public int RatingScore { get; internal set; }

        [JsonPropertyName("is_pinned")]
        [JsonInclude]
        public bool IsPinned { get; internal set; }

        [JsonPropertyName("is_deprecated")]
        [JsonInclude]
        public bool IsDeprecated { get; internal set; }

        [JsonPropertyName("total_downloads")]
        [JsonInclude]
        public int TotalDownloads { get; internal set; }

        [JsonPropertyName("latest")]
        [JsonInclude]
        public PackageVersion Latest { get; internal set; }

        [JsonPropertyName("community_listings")]
        [JsonInclude]
        public PackageListing[] CommunityListings { get; internal set; }

        public bool IsLatestVersion(string current)
        {
            if (string.IsNullOrWhiteSpace(current)) return false;
            if (this.Latest == null || this.Latest.SemanticVersion == null) return false;
            if (SemVersion.TryParse(current, out var version))
            {
                return version >= this.Latest.SemanticVersion;
            }
            return false;
        }

        public bool IsLatestVersion(SemVersion current)
        {
            if (current == null) return false;
            if (this.Latest == null || this.Latest.SemanticVersion == null) return false;
            return current >= this.Latest.SemanticVersion;
        }

        public bool IsLatestVersion(Version current)
        {
            if (current == null) return false;
            if (this.Latest == null || this.Latest.SemanticVersion == null) return false;
            return new SemVersion(current) >= this.Latest.SemanticVersion;
        }
    }

    public class PackageVersion
    {
        [JsonPropertyName("namespace")]
        [JsonInclude]
        public string Namespace { get; internal set; }

        [JsonPropertyName("name")]
        [JsonInclude]
        public string Name { get; internal set; }

        [JsonPropertyName("version_number")]
        [JsonInclude]
        public string Version
        { get { return SemanticVersion.ToString(); } internal set { SemanticVersion = Semver.SemVersion.Parse(value); } }

        [JsonIgnore]
        public SemVersion SemanticVersion { get; internal set; }

        [JsonPropertyName("full_name")]
        [JsonInclude]
        public string FullName { get; internal set; }

        [JsonPropertyName("description")]
        [JsonInclude]
        public string Description { get; internal set; }

        [JsonPropertyName("icon")]
        [JsonInclude]
        public string Icon { get; internal set; }

        [JsonPropertyName("dependencies")]
        [JsonInclude]
        public List<string> Dependencies { get; internal set; }

        [JsonPropertyName("download_url")]
        [JsonInclude]
        public string DownloadUrl { get; internal set; }

        [JsonPropertyName("date_created")]
        [JsonInclude]
        public DateTime CreatedAt { get; internal set; }

        [JsonPropertyName("downloads")]
        [JsonInclude]
        public int Downloads { get; internal set; }

        [JsonPropertyName("website_url")]
        [JsonInclude]
        public string WebsiteURL { get; internal set; }

        [JsonPropertyName("is_active")]
        [JsonInclude]
        public bool IsActive { get; internal set; }
    }

    public class PackageListing
    {
        [JsonPropertyName("has_nsfw_content")]
        [JsonInclude]
        public bool HasNSFWContent { get; internal set; }

        [JsonPropertyName("categories")]
        [JsonInclude]
        public List<string> Categories { get; internal set; }

        [JsonPropertyName("community")]
        [JsonInclude]
        public string Community { get; internal set; }

        [JsonPropertyName("review_status")]
        [JsonInclude]
        public string ReviewStatusString
        {
            get { return ReviewStatusValue.ToString(); }
            internal set
            {
                if (value == null) { throw new ArgumentNullException(nameof(value)); }
                else
                {
                    if (string.Equals(value, "unreviewed", StringComparison.OrdinalIgnoreCase)) ReviewStatusValue = ReviewStatus.UNREVIEWED;
                    else if (string.Equals(value, "approved", StringComparison.OrdinalIgnoreCase)) ReviewStatusValue = ReviewStatus.APPROVED;
                    else if (string.Equals(value, "rejected", StringComparison.OrdinalIgnoreCase)) ReviewStatusValue = ReviewStatus.REJECTED;
                }
            }
        }

        [JsonIgnore]
        public ReviewStatus ReviewStatusValue { get; internal set; }

        public enum ReviewStatus
        {
            UNREVIEWED = 0,
            APPROVED = 1,
            REJECTED = 2
        }
    }

    public class V1PackageMetrics
    {
        [JsonPropertyName("downloads")]
        [JsonInclude]
        public int Downloads { get; internal set; }

        [JsonPropertyName("rating_score")]
        [JsonInclude]
        public int RatingScore { get; internal set; }

        [JsonPropertyName("latest_version")]
        [JsonInclude]
        public string LatestVersion
        { get { return LatestSemanticVersion.ToString(); } internal set { LatestSemanticVersion = Semver.SemVersion.Parse(value); } }

        [JsonIgnore]
        public SemVersion LatestSemanticVersion { get; internal set; }

        public bool IsLatestVersion(string current)
        {
            if (string.IsNullOrWhiteSpace(current)) return false;
            if (this.LatestSemanticVersion == null) return false;
            if (SemVersion.TryParse(current, out var version))
            {
                return version >= this.LatestSemanticVersion;
            }
            return false;
        }

        public bool IsLatestVersion(SemVersion current)
        {
            if (current == null) return false;
            if (this.LatestSemanticVersion == null) return false;
            return current >= this.LatestSemanticVersion;
        }

        public bool IsLatestVersion(Version current)
        {
            if (current == null) return false;
            if (this.LatestSemanticVersion == null) return false;
            return new SemVersion(current) >= this.LatestSemanticVersion;
        }
    }

    public class ThunderstoreErrorResponse
    {
        [JsonPropertyName("detail")]
        [JsonInclude]
        public string Details { get; internal set; }
    }

    public class ThunderstoreErrorException : Exception
    {
        public ThunderstoreErrorException() : base()
        {
        }

        public ThunderstoreErrorException(string message) : base(message)
        {
        }

        public ThunderstoreErrorException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public ThunderstoreErrorException(string message, string details, HttpStatusCode httpStatusCode, Exception innerException) : base(message, innerException)
        {
            Details = details;
            HttpStatusCode = httpStatusCode;
        }

        public ThunderstoreErrorException(string message, HttpResponseMessage response) : base(message)
        {
            if (!response.IsSuccessStatusCode)
            {
                HttpStatusCode = response.StatusCode;
                var @string = response.Content.ReadAsStringAsync();
                @string.Wait();
                var _string = @string.Result;
                if (string.IsNullOrWhiteSpace(_string))
                {
                    Details = string.Empty;
                }
                else
                {
                    ThunderstoreErrorResponse error;
                    try
                    {
                        error = JsonSerializer.Deserialize<ThunderstoreErrorResponse>(_string);
                    }
                    catch (JsonException)
                    {
                        Details = string.Empty;
                        return;
                    }
                    if (error != null)
                    {
                        Details = error.Details;
                    }
                }
            }
        }

        public string Details { get; }
        public HttpStatusCode HttpStatusCode { get; }
    }

    public class ThunderstorePackageNotFoundException : ThunderstoreErrorException
    {
        public string Namespace { get; }
        public string Name { get; }
        public string Version { get; }

        public ThunderstorePackageNotFoundException(string message, string @namespace, string name, string details, HttpStatusCode httpStatusCode, Exception innerException) : base(message, details, httpStatusCode, innerException)
        {
            Namespace = @namespace;
            Name = name;
        }

        public ThunderstorePackageNotFoundException(string message, string @namespace, string name, string version, string details, HttpStatusCode httpStatusCode, Exception innerException) : base(message, details, httpStatusCode, innerException)
        {
            Namespace = @namespace;
            Name = name;
            Version = version;
        }

        public ThunderstorePackageNotFoundException(string message, string @namespace, string name, HttpResponseMessage response) : base(message, response)
        {
            Namespace = @namespace;
            Name = name;
        }

        public ThunderstorePackageNotFoundException(string message, string @namespace, string name, string version, HttpResponseMessage response) : base(message, response)
        {
            Namespace = @namespace;
            Name = name;
            Version = version;
        }

        public ThunderstorePackageNotFoundException() : base()
        {
        }

        public ThunderstorePackageNotFoundException(string message) : base(message)
        {
        }

        public ThunderstorePackageNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public ThunderstorePackageNotFoundException(string message, string details, HttpStatusCode httpStatusCode, Exception innerException) : base(message, details, httpStatusCode, innerException)
        {
        }

        public ThunderstorePackageNotFoundException(string message, HttpResponseMessage response) : base(message, response)
        {
        }
    }
}