# Jellyfin Plugin Wholphin

> [!WARNING]
> This plugin in still a work-in-progress and may be unstable!

This is a Jellyfin server plugin that provides extra functionality to [Wholphin](https://github.com/damontecres/Wholphin), a third-party Android TV client for Jellyfin.

This plugin allows for the server admin to pre-config the home page and provide Seerr integration information.

Please note: this plugin is not required to use Wholphin!

> [!NOTE]
> Wholphin `v1.1.0` or newer is required to make use of this plugin

## Installation

**This plugin requires Jellyfin 12.1 or newer.**

1. Open the Jellyfin Dashboard, go to Plugins
2. Click on `Manage Repositories`
3. Click `New Repository`
4. Enter the repository URL:
   ```
  https://raw.githubusercontent.com/liamfo993/jellyfin-plugin-wholphin/main/manifest.json
   ```
5. Go back to the Plugin page and click on `Available`
6. Find `Wholphin` in the list and click on it
7. Click `Install`
8. Restart Jellyfin to complete installation (on Dashboard page)
9. Go back to Dashboard->Plugins->Wholphin
10. Open `Settings` to configure the home screen and Seerr

The repository lists versions compatible with Jellyfin 12.1 and newer. If a
new release is not visible immediately, refresh the plugin catalog or check
that this repository is enabled.

### Publishing a release

Push a `vX.Y.Z` tag to build and publish the plugin ZIP and update
`manifest.json` on `main`. The release workflow needs permission to write
repository contents. Check that the workflow finishes successfully before
announcing the release.

## Configuration

### Jellyfin Dashboard

Open Dashboard -> Plugins -> Wholphin -> Settings. Add home screen rows, choose
their order and display options, and select libraries or media types from the
available options. Seerr can be configured with a server address and an
optional shared API key or local account. No YAML editing is needed for these
settings.

#### Kids movie rows

Select **Add kids movie rows** to add editable Family, Animated, Pixar, and Disney
rows. Each row can be renamed, reordered, removed, limited to a library, and
filtered by genre, studio, and maximum official rating. Separate alternative
genres or studios with `|`; when both are set, movies must match both.
Results depend on your library's metadata and each user's Jellyfin access.

These are discovery rows, not parental controls: animation and studio names
do not guarantee age suitability, and ratings can be missing or incorrect.
Configure Jellyfin parental controls and library access for children's accounts.
The rows appear to all users and do not change existing ratings or metadata.
In Wholphin, select the plugin as the home-screen settings source if local or
server-profile settings are taking precedence.

### Advanced YAML config

The plugin also supports YAML configuration for advanced and legacy setups.
Only basic syntax validations are performed on YAML input.

#### Example
```yaml
# Version of the settings
Version: 1
# Home page config
HomeConfig:
  HomePageSettings:
    # Version of the home page settings
    version: 1
    # Rows, they will appear in same order in the app
    rows:
      # A row for combined continue watching/next up items
      - type: ContinueWatchingCombined
      # A row for a collection (parent) sorted by name
      - type: ByParent
        parentId: <UUID>
        recursive: false
        sort:
          sort: SortName
          direction: Ascending
SeerrConfig:
  serverUrl: https://seerr.example.com
```

#### Home page rows

Sample YAML for home page rows

```yaml

# Continue watching row (not combined)
- type: ContinueWatching

# Next up row (not combined)
- type: NextUp

# Combined continue watching & next up
- type: ContinueWatchingCombined

# Recently added in a library or collection
- type: RecentlyAdded
  parentId: <UUID> # Library/Collection UUID

# Genres in a library
- type: Genres
  parentId: <UUID> # Library UUID

# Favorite shows
- type: Favorite
  kind: <kind> # Type of media (eg Series, Movie, Episode, Person, etc)

# Library, Collection, or playlist
- type: ByParent
  parentId: <UUID> # Library/Collection/Playlist ID
  recursive: true
  sort: # Optional
    sort: SortName
    direction: Ascending

# Items from an arbitrary Jellyfin endpoint that returns a QueryResult<BaseItemDto>.
# Lets other plugins (e.g. jellyfin-plugin-home-sections) feed rows into Wholphin
# without Wholphin needing to know about them.
#
# `userId` is injected automatically from the currently logged-in user. You can
# override it by including a `userId` entry in `query`.
- type: CustomEndpoint
  title: My Requests
  endpoint: /HomeScreen/Section/MyJellyseerrRequests  # relative to the Jellyfin baseUrl
  query:                                              # Optional extra query parameters
    - key: language
      value: en
  headers:                                            # Optional extra headers (auth token is added automatically)
    - key: X-Trace
      value: wholphin

```

#### Seerr config

When set, Wholphin will automatically configure its Seerr connection on first
login using these values. If `login` is omitted, only the URL is pushed and the
user supplies authentication themselves.

Two login types are supported when credentials are provided:

```yaml
# 1) API key — same login for everyone, no Seerr account needed per user
serverUrl: https://seerr.example.com
login:
  type: ApiKey
  apiKey: "<seerr-api-key>"
# 2) Local Seerr account — shared local Seerr login for everyone
serverUrl: https://seerr.example.com
login:
  type: Local
  local:
    username: "household@example.com"
    password: "..."
```

## Acknowledgements

- Credit to @kamilkosek: some code and inspiration are taken from their original idea for for a [Wholphin plugin](https://github.com/kamilkosek/jellyfin-plugin-wholphin)
