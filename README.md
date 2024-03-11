# Knowit.Umbraco.Dictionoid

Dictionoid integrates OpenAI's language capabilities with Umbraco, streamlining the translation process within the platform. 
By leveraging OpenAI's Completion API and requiring an API key for access, Dictionoid simplifies the management of multilingual content for developers and content managers. 
Particularly useful for projects requiring localization in multiple languages, it significantly reduces the manual effort of creating translations.

Dictionoid works in Umbraco 10, 11, 12 and 13

## Features

- **Automatic Translations**: With a simple button click, Dictionoid translates your text into all languages set up in Umbraco, streamlining content localization.
- **Hover To See Translations**: The package extends the default view of Umbraco's Translate section, to show tranlation-values on hover.
- **Translation History**: Track changes to your dictionary items, maintaining a record of modifications over time for better version control.
- **Code-First Dictionary Generation**: For developers, Dictionoid offers the ability to generate dictionary items directly from code using `@await Umbraco.Dictionoid("My dictionary item", "my.key")`, eliminating the need to manually create items in the Backoffice.
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

### Hover to See Translation

The "Hover to See Translation" feature enhances the user experience within the Umbraco Backoffice, specifically in the translation section. It is designed to streamline the process of reviewing translations by providing immediate visibility. When a user hovers over an item in the main translation table, this feature displays a tooltip showing the translation content for that particular item. This allows for quick verification and comparison of translations across different languages without the need to click into each dictionary item individually.

### Code-First Dictionary Items

The `CreateOnNotExist` feature facilitates a code-first approach, allowing developers to define dictionary items within their codebase. If a specified key does not exist, Dictionoid auto-generates it and populates translations using OpenAI, streamlining the development workflow.

### CleanupInBackoffice

The `CleanupInBackoffice` feature provides a user-friendly interface within Umbraco's Backoffice to facilitate the cleanup process of your dictionary items. By introducing a dedicated button in the translation section, this feature allows users to effortlessly iterate through every `.cshtml` file under the `/Views` directory. When activated, `CleanupInBackoffice` searches for instances where `Umbraco.Dictionoid` has been used and, if the associated dictionary item exists, replaces this code with the standard `Umbraco.GetDictionaryValue` method. This ensures that your code remains clean and maintainable, particularly useful for tidying up after bulk dictionary item generation or when preparing your project for production deployment.

### TrackHistory

The `TrackHistory` functionality addresses a common limitation in Umbraco regarding dictionary item modifications: once a dictionary item is altered, its previous state is typically lost. With `TrackHistory` enabled, Dictionoid creates a new database table, `KnowitDictionoidHistory`, to log every change made to dictionary items. This includes capturing both the old and new values whenever an update occurs. Accessible through the dictionary item editing interface, this history allows developers and content managers to review and revert changes if necessary, providing an invaluable audit trail and enhancing content integrity over time.


### CleanupAfterCreate

The `CleanupAfterCreate` feature is designed for developers to ensure their source code remains clean and straightforward following the use of Dictionoid's code-first generation capabilities. Once a dictionary item is successfully created and translations are populated, this feature automates the process of replacing `@await Umbraco.Dictionoid` calls with the conventional `@Umbraco.GetDictionaryValue` in your source code. This transformation occurs in your `.cshtml` files, ensuring that future references to these dictionary items adhere to standard Umbraco practices. Enabling `CleanupAfterCreate` is especially beneficial during the development phase, helping maintain a clean codebase and ensuring that your dictionary item references are always up-to-date. However, it is recommended to use this feature cautiously and review changes it makes, particularly when used in production environments, to avoid unintended consequences.

### Obtaining an OpenAI API Key

To leverage the translation and language processing features of Dictionoid, you'll need to obtain an API key from OpenAI. Here's a straightforward guide to help you secure your API key:

1. **Create an OpenAI Account**: Visit [OpenAI's website](https://www.openai.com/) and sign up for an account if you don't already have one.
2. **Access the API Section**: Once logged in, navigate to the API section, typically found in your account dashboard or under a designated API menu.
3. **Register Your Application**: You might need to register your application and describe its purpose. This step helps OpenAI understand how their API will be used.
4. **Generate API Key**: Follow the prompts to generate a new API key. This key will serve as your unique identifier and token for accessing the OpenAI services.
5. **Secure Your API Key**: Store your API key securely and avoid sharing it publicly. Treat it like a password, as it provides access to your OpenAI account and billing information.

Once you have your API key, you can input it into your `appsettings.json` under `Knowit.Umbraco.Dictionoid` to enable the integration within your Umbraco project.


## License

Dictionoid is made available under the [MIT License](LICENSE), which permits broad usage, including modification, distribution, and private or commercial use, while requiring only attribution and copyright notice retention.

As Dictionoid is in its early stages, users should anticipate the possibility of encountering bugs. We welcome feedback and contributions to enhance its reliability and feature set.


### Acknowledgment for OpenAI-API-dotnet

This project utilizes the OpenAI-API-dotnet library, which is dedicated to the public domain under the CC0 1.0 Universal (CC0 1.0) Public Domain Dedication. We appreciate the contributions made by the developers of OpenAI-API-dotnet.

More information about the OpenAI-API-dotnet library can be found at: https://github.com/OkGoDoIt/OpenAI-API-dotnet

More information about the CC0 1.0 license is available at: https://creativecommons.org/publicdomain/zero/1.0/
