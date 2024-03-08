# Knowit.Umbraco.Dictionoid

Dictionoid integrates OpenAI's language capabilities with Umbraco, streamlining the translation process within the platform. By leveraging OpenAI's Completion API and requiring an API key for access, Dictionoid simplifies the management of multilingual content for developers and content managers. Particularly useful for projects requiring localization in multiple languages, it significantly reduces the manual effort of creating translations, benefiting projects that commonly pair local languages with English.

## Features

- **Automatic Translations**: With a simple button click, Dictionoid translates your text into all languages set up in Umbraco, streamlining content localization.
- **Translation History**: Track changes to your dictionary items, maintaining a record of modifications over time for better version control.
- **Code-First Dictionary Generation**: For developers, Dictionoid offers the ability to generate dictionary items directly from code using `@await Umbraco.Dictionoid("My dictionary item", "my.key")`, eliminating the need to manually create items in the back office.
- **Source Code Integration**: Seamlessly integrates with your Umbraco projects, offering features like dictionary item tracking and source code cleanup post-item creation.
- **Opt-in Features**: Additional developer-centric functionalities such as tracking history, code cleanup, and code-first generation are opt-in to ensure they are used judiciously, particularly outside production environments.

## Setup / Configuration

To get started, include the minimum required configuration in your `appsettings.json`:

```json
"Knowit.Umbraco.Dictionoid": {
  "ApiKey": "a valid OpenAI API key",
}
```

For developers seeking to leverage advanced features, the full configuration is as follows:

```json
"Knowit.Umbraco.Dictionoid": {
  "ApiKey": "a valid OpenAI API key",
  "CreateOnNotExist": true,
  "CleanupAfterCreate": false,
  "CleanupInBackoffice": true,
  "TrackHistory":  true
}
```

**Note:** Features like `CreateOnNotExist`, `CleanupAfterCreate`, and `CleanupInBackoffice` are powerful tools that modify source code and should be used with caution, ideally outside of production environments, to avoid unintended alterations.

### Code-First Dictionary Items

The `CreateOnNotExist` feature facilitates a code-first approach, allowing developers to define dictionary items within their codebase. If a specified key does not exist, Dictionoid auto-generates it and populates translations using OpenAI, streamlining the development workflow.

## License

Dictionoid is made available under the [MIT License](LICENSE), which permits broad usage, including modification, distribution, and private or commercial use, while requiring only attribution and copyright notice retention.

As Dictionoid is in its early stages, users should anticipate the possibility of encountering bugs. We welcome feedback and contributions to enhance its reliability and feature set.


### Acknowledgment for OpenAI-API-dotnet

This project utilizes the OpenAI-API-dotnet library, which is dedicated to the public domain under the CC0 1.0 Universal (CC0 1.0) Public Domain Dedication. We appreciate the contributions made by the developers of OpenAI-API-dotnet.

More information about the OpenAI-API-dotnet library can be found at: https://github.com/OkGoDoIt/OpenAI-API-dotnet

More information about the CC0 1.0 license is available at: https://creativecommons.org/publicdomain/zero/1.0/
