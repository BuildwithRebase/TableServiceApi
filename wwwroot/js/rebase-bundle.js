'use strict';

var RebaseConfig = /** @class */ (function () {
    function RebaseConfig() {
        this.baseUrl = '';
    }
    return RebaseConfig;
}());

var RebaseComponent = /** @class */ (function () {
    function RebaseComponent(rebaseConfig) {
        this.rebaseTemplates = {};
        this.model = {};
        this.tableName = '';
        this.mode = '';
        this.token = '';
        this.rebaseConfig = rebaseConfig;
    }
    RebaseComponent.prototype.isLoggedOn = function () {
        var rebaseUser = window.localStorage.getItem('rebaseUser');
        if (rebaseUser == null) {
            return false;
        }
        this.token = JSON.parse(rebaseUser).token;
        return true;
    };
    /**
     * Initialises the Rebase Controls
     */
    RebaseComponent.prototype.initialise = function () {
        var _this = this;
        console.log('Loading rebase');
        var self = this;
        if (Object.keys(this.rebaseTemplates).length == 0) {
            this.loadTemplates();
        }
        if (this.isLoggedOn() && !(window.location.pathname === '/login') && !(window.location.pathname === '/invoices')) {
            // window.billflowSettings 
            if (window['billflowSettings'] && window.localStorage.getItem('rebaseEmail')) {
                window['billflowSettings']['email'] = window.localStorage.getItem('rebaseEmail');
            }
            $('.rebase-signout-link').on('click', function () {
                self.signout();
            });
            this.getData();
            $('[data-rebase-action]').each(function (index, el) {
                $(el).on('click', function () {
                    _this[$(el).data('rebaseAction')](self);
                });
            });
        } else if (window.location.pathname === '/invoices') {
            // going to load something here
            window.ts.raiseEvent = false;
            window.ts.token = this.token;
            window.ts.accounts.getHMAC()
                .then(function (hmac) {
                    window.billflowSettings['hash'] = hmac.message;
                });
        } else {
            this.loadLogin();
        }
    };
    RebaseComponent.prototype.signout = function () {
        window.localStorage.removeItem('rebaseUser');
        window.ts.users.logout()
            .then(() => console.log('logged out'));

        window.location.href = '/login';
        if (window['servicebotSettings']) {
            window['servicebotSettings']['email'] = '';
        }
        if (window.localStorage.getItem('rebaseEmail')) {
            window.localStorage.removeItem('rebaseEmail');
        }
    };
    RebaseComponent.prototype.loadLogin = function () {
        var _this = this;
        var self = this;
        if (window.location.pathname === '/login') {
            $('.rebase-login-btn').attr('type', 'button');
            $('.rebase-login-btn').on('click', function () { return _this.login(self); });
        }
        else {
            window.location.href = '/login';
        }
    };
    RebaseComponent.prototype.loadTemplates = function () {
        var _this = this;
        console.log('loadTemplates');
        $('.rebase-control').each(function (index, rebaseControl) {
            var html = $(rebaseControl).html();
            if ($(rebaseControl).hasClass('rebase-control-table')) {
                // find the table row
                var rowTmpl = $(rebaseControl).find('[data-rebase-item]')[0];
                var table = $(rowTmpl).data('rebaseItem').split('.')[0];
                _this.tableName = table;
                var text = "{{#each " + table + "}}" + $(rowTmpl).parent()[0].innerHTML + "{{/each}}";
                console.log(text);
                _this.rebaseTemplates[table] = {
                    template: Handlebars.compile(text),
                    container: $(rowTmpl).parent()[0],
                    fields: null
                };
                _this.rebaseTemplates[table].container.removeChild(rowTmpl);
            }
            if ($(rebaseControl).hasClass('rebase-control-form')) {
                // find the form
                var formTmpl = $(rebaseControl).find('[data-rebase-item]')[0];
                var form = $(formTmpl).data('rebaseItem').split('.')[0];
                var fields_1 = [];
                $(formTmpl).find('.rebase-control').each(function (index, rc) {
                    fields_1.push(rc.id);
                });
                _this.rebaseTemplates[form + 'Form'] = {
                    template: Handlebars.compile(formTmpl.innerHTML),
                    container: $(formTmpl).parent()[0],
                    fields: fields_1
                };
                _this.rebaseTemplates[form + 'Form'].container.removeChild(formTmpl);
            }
        });
    };
    RebaseComponent.prototype.getData = function () {
        var _this = this;
        window.ts.token = this.token;
        window.ts.raiseEvent = false;

        window.ts.dataItems.getTableDataItems(this.tableName, 1, 10)
            .then(function (response) {
                _this.model[_this.tableName] = response.data;
                // const output = this.rebaseTemplates[this.tableName].template(this.model);
                if (Object.keys(_this.rebaseTemplates)) {
                    var output = processTemplate(_this.rebaseTemplates[_this.tableName].template, _this.model);
                    _this.rebaseTemplates[_this.tableName].container.innerHTML = output;
                }
                setTimeout(function () {
                    $('.rebase-button').each(function (index, rebaseButton) {
                        if ('+' == rebaseButton.innerHTML) {
                            $(rebaseButton).on('click', function ($ev) { return _this.editData($ev.target); });
                        }
                        else if ('-' == rebaseButton.innerHTML) {
                            $(rebaseButton).on('click', function ($ev) { return _this.deleteRecord($ev.target); });
                        }
                    });
                }, 300);
            })["catch"](function (error) { return console.log(error); });
    };

    RebaseComponent.prototype.editData = function (el) {
        var index = $(el).data('index');
        var form = this.tableName + 'Form';
        var data = this.model[this.tableName][index];
        var text = this.rebaseTemplates[form].template(data);
        this.rebaseTemplates[form].container.innerHTML = text;
        this.mode = 'edit';
    };
    RebaseComponent.prototype.deleteRecord = function (el) {
        this.deleteId = $(el).data('id');
        this.mode = 'delete';
    };
    RebaseComponent.prototype.createData = function (self) {
        console.log('createData');
        var data = {};
        var form = self.tableName + 'Form';
        var idFld = self.tableName.substring(0, self.tableName.length - 1) + 'Id';
        $(self.rebaseTemplates[form].fields).each(function (index, value) {
            data[value] = '';
        });
        data[idFld] = uuidv4().toUpperCase();
        console.log(data);
        var text = self.rebaseTemplates[form].template(data);
        self.rebaseTemplates[form].container.innerHTML = text;
        self.mode = 'save';
    };
    RebaseComponent.prototype.saveData = function (self) {
        console.log('saveData');
        var data = {};
        var form = self.tableName + 'Form';
        $(self.rebaseTemplates[form].container).find('.rebase-control').each(function (index, rebaseControl) {
            data[rebaseControl.id] = rebaseControl.value;
        });

        window.ts.token = self.token;
        window.ts.raiseEvent = false;


        if (self.mode === 'edit') {
            window.ts.dataItems.updateTableDataItem(self.tableName, data.id, data)
                .then(function (response) {
                    console.log(response);
                    self.getData();
                })["catch"](function (error) { return console.log(error); });
        } else {
            window.ts.dataItems.addTableDataItem(self.tableName, data)
                .then(function (response) {
                    console.log(response);
                    self.getData();
                })["catch"](function (error) { return console.log(error); });
        }
    };
    RebaseComponent.prototype.deleteData = function (self) {
        window.ts.token = this.token;
        window.ts.raiseEvent = false;

        window.ts.dataItems.deleteTableDataItem(self.tableName, self.deleteId)
            .then(function (response) {
                console.log(response);
                self.getData();
            })["catch"](function (error) { return console.log(error); });
    };
    RebaseComponent.prototype.login = function (self) {
        var email = $('.rebase-login-email').val();
        var password = $('.rebase-login-password').val();

        window.ts.raiseEvent = false;
        window.ts.users.authenticateUser(email, password)
            .then(user => {
                window.localStorage.setItem('rebaseUser', JSON.stringify(user));
                window.location.href = '/';
            });
    };
    return RebaseComponent;
}());

function processTemplate(template, data) {
    return template(data);
}

(function () {
    var rebaseConfig = new RebaseConfig();
    // configure the rebase control
    // to-do: make this dynamic
    rebaseConfig.baseUrl = '/api';
    var rebaseComponent = new RebaseComponent(rebaseConfig);
    window['rebaseComponent'] = rebaseComponent;

    var pageController = function () {
        rebaseComponent.initialise();
    };
    pageController();
})();
