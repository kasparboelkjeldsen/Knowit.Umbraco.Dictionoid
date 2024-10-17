(function () {

    angular.module('umbraco').directive('umbDictionoidEdit', function () {
        return {
            restrict: 'E',
            replace: true,
            controller: 'DictionoidEditController',
            controllerAs: 'ctrl',
            templateUrl: '/App_Plugins/Knowit.Umbraco.Dictionoid/views/dictionoid.edit.html'
        };
    });

    angular.module('umbraco').directive('umbDictionoidList', function () {
        return {
            restrict: 'E',
            replace: true,
            controller: 'DictionoidListController',
            controllerAs: 'ctrl',
            templateUrl: '/App_Plugins/Knowit.Umbraco.Dictionoid/views/dictionoid.list.html'
        };
    });

    angular.module('umbraco').directive('tip', function () {
        return {
            restrict: 'A',
            scope: { tip: '@tip' },
            controller: function ($scope, $http) {
                var ctrl = this;
                ctrl.items = [];
                $http.get("/umbraco/backoffice/dictionoid/getall")
                    .then(response => {
                        ctrl.items = response.data.map(d => ({
                            languageCultureName: d.languageCultureName,
                            value: d.value,
                            key: d.key
                        }));
                        $scope.$broadcast("Data_Ready");
                    });
            },
            controllerAs: 'ctrl',
            link: function (scope, element) {
                scope.$on("Data_Ready", function () {
                    var item = scope.ctrl.items.find(i => i.languageCultureName === scope.$parent.column.displayName && i.key === scope.$parent.$parent.item.name);
                    scope.tip = (item && item.value) ? item.value : 'No translation available.';
                    var tooltip = '<span class="tip">' + scope.tip + '</span>';
                    element.append(tooltip);
                    element.children().first().bind('mouseenter mouseleave', function () {
                        element.children(".tip").toggleClass('active');
                    });
                });
            }
        };
    });

    angular.module('umbraco').controller('DictionoidListController', function ($http, $scope) {
        var self = this; // Brug 'var' for at definere 'self'

        self.baseCtrl = $scope.vm;
        angular.extend(self, self.baseCtrl);

        const DEFAULT_FEEDBACK = " - This will modify the code in your views!";
        const RESPONSE_FEEDBACK = "Your code might have been modified. If new keys have been created, you might need to restart Umbraco.";

        self.feedback = DEFAULT_FEEDBACK;
        self.showConfirmButton = false;
        self.showCleanupButton = true;
        self.changes = [];
        self.loading = false;

        self.confirm = function () {
            self.showConfirmButton = true;
            self.showCleanupButton = false;
        };

        self.cleanup = function () {
            self.loading = true;

            $http.get(`/umbraco/backoffice/dictionoid/cleanupinspect`)
                .then(response => {
                    self.changes = Object.entries(response.data).map(([file, keys]) => ({ file, keys }));
                }).finally(() => {
                    self.loading = false;
                    self.showConfirmButton = false;
                    self.feedback = RESPONSE_FEEDBACK;
                });
        };

        // Clear cache on initialization
        $http.get("/umbraco/backoffice/dictionoid/clearcache");
        $http.get(`/umbraco/backoffice/dictionoid/shouldcleanup`)
            .then(response => self.shouldCleanup = response.data);
    });

    angular.module('umbraco').controller('DictionoidEditController', function ($http, $scope, IsAiDisabled) {
        var self = this; // Brug 'var' for at definere 'self'

        self.baseCtrl = $scope.vm;
        angular.extend(self, self.baseCtrl);

        self.color = "";
        self.isButtonDisabled = false;
        self.changes = [];
        self.aiDisabled = true; // Korrekt navngivning

        IsAiDisabled.check().then(function (response) {
            self.aiDisabled = response; // Brug 'aiDisabled' her
        });

        self.translate = function () {
            self.isButtonDisabled = true;

            var data = {
                color: self.color,
                items: self.baseCtrl.content.translations.map(item => ({
                    value: item.translation,
                    id: item.isoCode,
                    key: item.displayName
                }))
            };

            $http({
                method: 'POST',
                url: '/umbraco/backoffice/dictionoid/translate',
                headers: { 'Content-Type': 'application/json' },
                data: data
            }).then(function (response) {
                self.baseCtrl.content.translations = self.baseCtrl.content.translations.map(t => {
                    var found = response.data.Items.find(i => i.Key === t.displayName);
                    if (found) {
                        t.translation = found.Value;
                    }
                    return t;
                });
            }).finally(function () {
                self.isButtonDisabled = false;
            });
        };

        self.getHistory = function () {
            $http.get('/umbraco/backoffice/dictionoid/history?key=' + self.baseCtrl.content.name)
                .then(function (response) {
                    self.changes = response.data;
                });
        };

        $scope.$watch(
            function () { return self.baseCtrl.content?.name; },
            function (newValue) {
                if (newValue) {
                    self.getHistory();
                }
            }
        );
    });

    angular.module('umbraco').service('IsAiDisabled', function ($http) {
        this.check = function () {
            return $http.get('/umbraco/backoffice/dictionoid/isaidisabled')
                .then(function (response) {
                    return response.data;
                })
                .catch(function (error) {
                    console.error('Fejl ved kald til API', error);
                    return false; // Return false ved fejl
                });
        };
    });

    angular.module('umbraco.services').config([
        "$httpProvider",
        function ($httpProvider) {
            $httpProvider.interceptors.push(['$q', function ($q) {
                return {
                    'response': function (response) {
                        if (response.config.url.startsWith('views/dictionary/edit.htm')) {
                            response.data = response.data.replace('</umb-editor-container>', '<umb-dictionoid-edit></umb-dictionoid-edit></umb-editor-container>');
                        }

                        if (response.config.url.startsWith('views/dictionary/list.htm')) {
                            response.data = response.data.replace('<umb-editor-container>', '<umb-editor-container><umb-dictionoid-list></umb-dictionoid-list>');
                            response.data = response.data.replace('<td ng-repeat="column in item.translations',
                                '<td tip="" ng-repeat="column in item.translations');
                        }

                        return response || $q.when(response);
                    }
                };
            }]);
        }
    ]);
})();
