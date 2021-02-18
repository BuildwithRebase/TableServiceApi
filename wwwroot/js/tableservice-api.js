

/**
 * Generic version of the table service Api Client that can be to used to access the TableService back end
 * https://appcore.buildwithrebase.online/api in production
 * 
 * The library is designed to work by the functions being invoked as standard javascript functions 
 * and then a custom event will be posted back for a consuming application to handle it
 * i.e.
 * <code>
 * 
 *  document.addEventListener('ts_result', function(result) => console.log(result));
 *  
 *  var ts = new TableServiceApiClient();
 *  ts.ping();
 * 
 * </code>
 * 
 */
var TableServiceApiClient = function () {

    this.baseUrl = (window.location.href.indexOf('localhost') > -1) ? 'https://localhost:5001/api' : 'https://appcore.buildwithrebase.online/api';
    this.token = null;
    this.raiseEvent = true;
    this.lastStatus = 0;
    this.lastError = '';

    this.isAuthenticated = function () {
        return (this.token != null);
    }

    this.invalidToken = function () {
        this.token = null;
    }

    this.setRaiseEvent = function (raiseEvent) {
        this.raiseEvent = raiseEvent;
    }

    this.apiHandleResult = function apiHandleResult(fn, result) {
        var detail = {
            fn: fn,
            result: result
        }

        if (fn === 'authenticateUser') {
            this.token = result.token;
        } else if (fn === 'logout') {
            this.invalidToken();
        }

        var event = new CustomEvent("ts_result", {
            detail: detail,
            bubbles: true,
            cancelable: true
        });
        document.body.dispatchEvent(event);
    }

    this.apiHandleError = function apiHandleError(fn, error) {
        var event = new CustomEvent("ts_error", {
            detail: {
                fn: fn,
                result: result
            },
            bubbles: true,
            cancelable: true
        });

        document.body.dispatchEvent(event);
    }

    this.createUrl = function createUrl(resource, page, pageSize) {
        var url = '/' + resource;
        if (page && pageSize) {
            url += '?page=' + page + '&pageSize=' + pageSize;
        } else if (!page && pageSize) {
            url += '?pageSize=' + pageSize;
        } else if (page && !pageSize) {
            url += '?page=' + page;
        }
        return url;
    }

    this.internalFetch = function internalFetch(fn, url, requestOptions) {

        if (this.token) {
            requestOptions.headers = new Headers({
                'Authorization': 'Bearer ' + this.token,
                'Content-Type': 'application/json'
            });
        } else if (window.localStorage.getItem('user') != null) {
            var user = JSON.parse(window.localStorage.getItem('user'));
            this.token = user.token;
            requestOptions.headers = new Headers({
                'Authorization': 'Bearer ' + this.token,
                'Content-Type': 'application/json'
            });
        }



        if (this.raiseEvent) {
            fetch(this.baseUrl + url, requestOptions)
                .then(response => response.json())
                .then(result => this.apiHandleResult(fn, result))
                .catch(error => this.apiHandleError(fn, error));

            return 1;
        } else {
            return fetch(this.baseUrl + url, requestOptions)
                .then(response => {
                    this.lastStatus = response.status;
                    if (response.status > 400) {
                        return response.text();
                    }
                    return response.json()
                });
        }
    }

    this.ping = function ping() {
        var requestOptions = {
            method: 'GET',
            redirect: 'follow'
        };

        return this.internalFetch(arguments.callee.name, '/Ping', requestOptions);
    }

    // assign routes
    this.accounts = new Accounts(this);
    this.sessions = new ApiSession(this);
    this.dataItems = new TsData(this);
    this.tables = new TsTables(this);
    this.teams = new TsTeams(this);
    this.users = new TsUsers(this);
}

var Accounts = function (tsClient) {
        this.tsClient = tsClient;

    this.getAccount = function getAccount() {
        var myHeaders = new Headers();
        myHeaders.append("Authorization", "true");

        var requestOptions = {
            method: 'GET',
            headers: myHeaders,
            redirect: 'follow'
        };

        var url = '/Account';
        return this.tsClient.internalFetch(arguments.callee.name, url, requestOptions);
    }

    this.updateBillFlowSecret = function updateBillFlowSecret(billFlowSecret) {
        var myHeaders = new Headers();
        myHeaders.append("Authorization", "true");

        var raw = { billFlowSecret: billFlowSecret }

        var requestOptions = {
            method: 'POST',
            headers: myHeaders,
            redirect: 'follow',
            body: JSON.stringify(raw)
        };

        var url = '/Account/updateBillFlowSecret';
        return this.tsClient.internalFetch(arguments.callee.name, url, requestOptions);
    }

    this.getHMAC = function getAccount() {
        var myHeaders = new Headers();
        myHeaders.append("Authorization", "true");

        var requestOptions = {
            method: 'GET',
            headers: myHeaders,
            redirect: 'follow'
        };

        var url = '/Account/getBillFlowHMAC';
        return this.tsClient.internalFetch(arguments.callee.name, url, requestOptions);
    }
}

/**
 * Represents the Api Session
 */
var ApiSession = function (tsClient) {
    this.tsClient = tsClient;

    this.getById = function apiSession(id) {
        var myHeaders = new Headers();
        myHeaders.append("Authorization", "true");

        var requestOptions = {
            method: 'GET',
            headers: myHeaders,
            redirect: 'follow'
        };

        var url = '/ApiSessions/' + id;

        return this.tsClient.internalFetch(arguments.callee.name, url, requestOptions);
    }

    this.getApiSessions = function apiSessions(page, pageSize) {
        var myHeaders = new Headers();
        myHeaders.append("Authorization", "true");

        var requestOptions = {
            method: 'GET',
            headers: myHeaders,
            redirect: 'follow'
        };

        var url = this.tsClient.createUrl('ApiSessions', page, pageSize);
        return this.tsClient.internalFetch(arguments.callee.name, url, requestOptions);
    }

    this.revokeSession = function revokeSession(id) {
        var myHeaders = new Headers();
        myHeaders.append("Authorization", "true");

        var requestOptions = {
            method: 'PUT',
            headers: myHeaders,
            redirect: 'follow'
        };

        var url = '/ApiSessions/' + id + '/revoke';
        return this.tsClient.internalFetch(arguments.callee.name, url, requestOptions);
    }

}

var TsData = function (tsClient) {

    this.tsClient = tsClient;

    this.getTeamEntities = function getTeamEntities () {
        var myHeaders = new Headers();
        myHeaders.append("Authorization", "true");

        var requestOptions = {
            method: 'GET',
            headers: myHeaders,
            redirect: 'follow'
        };

        var url = '/Data';
        return this.tsClient.internalFetch(arguments.callee.name, url, requestOptions);
    }
    
    this.getTableDataItem = function getTableDataItem(tableName, id) {
        var myHeaders = new Headers();
        myHeaders.append("Authorization", "true");

        var requestOptions = {
            method: 'GET',
            headers: myHeaders,
            redirect: 'follow'
        };

        var url = '/Data/' + tableName + '/' + id;
        return this.tsClient.internalFetch(arguments.callee.name, url, requestOptions);
    }

    this.updateTableDataItem = function updateTableDataItem(tableName, id, dataItem) {
        var myHeaders = new Headers();
        myHeaders.append("Content-Type", "application/json");
        myHeaders.append("Authorization", "true");

        var raw = JSON.stringify(dataItem);

        var requestOptions = {
            method: 'PUT',
            headers: myHeaders,
            body: raw,
            redirect: 'follow'
        };

        var url = '/Data/' + tableName + '/' + id;
        return this.tsClient.internalFetch(arguments.callee.name, url, requestOptions);
    }

    this.deleteTableDataItem = function deleteTableDataItem(tableName, id) {
        var myHeaders = new Headers();
        myHeaders.append("Authorization", "true");

        var requestOptions = {
            method: 'DELETE',
            headers: myHeaders,
            redirect: 'follow'
        };

        var url = '/Data/' + tableName + '/' + id;
        return this.tsClient.internalFetch(arguments.callee.name, url, requestOptions);
    }

    this.getTableDataItems = function getTableDataItems(tableName, page, pageSize) {
        var myHeaders = new Headers();
        myHeaders.append("Authorization", "true");

        var requestOptions = {
            method: 'GET',
            headers: myHeaders,
            redirect: 'follow'
        };

        var url = this.tsClient.createUrl('Data/' + tableName, page, pageSize);
        return this.tsClient.internalFetch(arguments.callee.name, url, requestOptions);
    }

    this.addTableDataItem = function addTableDataItem(tableName, dataItem) {
        var myHeaders = new Headers();
        myHeaders.append("Content-Type", "application/json");
        myHeaders.append("Authorization", "true");

        var raw = JSON.stringify(dataItem);

        var requestOptions = {
            method: 'POST',
            headers: myHeaders,
            body: raw,
            redirect: 'follow'
        };

        var url = '/Data/' + tableName;
        return this.tsClient.internalFetch(arguments.callee.name, url, requestOptions);
    }
}


var TsTables = function (tsClient) {

    this.tsClient = tsClient;

    this.getTables = function getTables(page, pageSize) {
        var myHeaders = new Headers();
        myHeaders.append("Authorization", "true");

        var requestOptions = {
            method: 'GET',
            headers: myHeaders,
            redirect: 'follow'
        };

        var url = this.tsClient.createUrl('Tables', page, pageSize);
        return this.tsClient.internalFetch(arguments.callee.name, url, requestOptions);
    }

    this.addTable = function addTable(data) {
        var myHeaders = new Headers();
        myHeaders.append("Content-Type", "application/json");
        myHeaders.append("Authorization", "true");

        var raw = JSON.stringify(data);

        var requestOptions = {
            method: 'POST',
            headers: myHeaders,
            body: raw,
            redirect: 'follow'
        };

        var url = '/Tables';
        return this.tsClient.internalFetch(arguments.callee.name, url, requestOptions);
    }

    this.getTable = function getTable(id) {
        var myHeaders = new Headers();
        myHeaders.append("Authorization", "true");

        var requestOptions = {
            method: 'GET',
            headers: myHeaders,
            redirect: 'follow'
        };

        var url = '/Tables/' + id;
        return this.tsClient.internalFetch(arguments.callee.name, url, requestOptions);
    }

    this.updateTable = function updateTable(id, data) {
        var myHeaders = new Headers();
        myHeaders.append("Content-Type", "application/json");
        myHeaders.append("Authorization", "true");

        var raw = JSON.stringify(data);

        var requestOptions = {
            method: 'PUT',
            headers: myHeaders,
            body: raw,
            redirect: 'follow'
        };

        var url = '/Tables/' + id;
        return this.tsClient.internalFetch(arguments.callee.name, url, requestOptions);
    }

    this.deleteTable = function deleteTable(id) {
        var myHeaders = new Headers();
        myHeaders.append("Authorization", "true");

        var requestOptions = {
            method: 'DELETE',
            headers: myHeaders,
            redirect: 'follow'
        };

        var url = '/Tables/' + id;
        return this.tsClient.internalFetch(arguments.callee.name, url, requestOptions);
    }
}

var TsTeams = function (tsClient) {

    this.tsClient = tsClient;

    this.getTeams = function getTeams(page, pageSize) {
        var myHeaders = new Headers();
        myHeaders.append("Authorization", "true");

        var requestOptions = {
            method: 'GET',
            headers: myHeaders,
            redirect: 'follow'
        };

        var url = this.tsClient.createUrl('Teams', page, pageSize);
        return this.tsClient.internalFetch(arguments.callee.name, url, requestOptions);
    }

    this.addTeam = function addTeam() {
        var myHeaders = new Headers();
        myHeaders.append("Content-Type", "application/json");
        myHeaders.append("Authorization", "true");

        var raw = JSON.stringify(data);

        var requestOptions = {
            method: 'POST',
            headers: myHeaders,
            body: raw,
            redirect: 'follow'
        };

        var url = '/Teams';
        return this.tsClient.internalFetch(arguments.callee.name, url, requestOptions);
    }

    this.getTeam = function getTeam(id) {
        var myHeaders = new Headers();
        myHeaders.append("Authorization", "true");

        var requestOptions = {
            method: 'GET',
            headers: myHeaders,
            redirect: 'follow'
        };

        var url = '/Teams/' + id;
        return this.tsClient.internalFetch(arguments.callee.name, url, requestOptions);
    }

    this.updateTeam = function updateTeam(id, data) {
        var myHeaders = new Headers();
        myHeaders.append("Content-Type", "application/json");
        myHeaders.append("Authorization", "true");

        var raw = JSON.stringify(data);

        var requestOptions = {
            method: 'PUT',
            headers: myHeaders,
            body: raw,
            redirect: 'follow'
        };

        var url = '/Teams/' + id;
        return this.tsClient.internalFetch(arguments.callee.name, url, requestOptions);
    }

    this.deleteTeam = function deleteTeam(id) {
        var myHeaders = new Headers();
        myHeaders.append("Authorization", "true");

        var requestOptions = {
            method: 'DELETE',
            headers: myHeaders,
            redirect: 'follow'
        };

        var url = '/Teams/' + id;
        return this.tsClient.internalFetch(arguments.callee.name, url, requestOptions);
    }
}

var TsUsers = function (tsClient) {

    this.tsClient = tsClient;

    this.getUsers = function getUsers(page, pageSize) {
        var myHeaders = new Headers();
        myHeaders.append("Authorization", "true");

        var requestOptions = {
            method: 'GET',
            headers: myHeaders,
            redirect: 'follow'
        };

        var url = this.tsClient.createUrl('Users', page, pageSize);
        return this.tsClient.internalFetch(arguments.callee.name, url, requestOptions);
    }

    this.getUser = function getUser(id) {
        var myHeaders = new Headers();
        myHeaders.append("Authorization", "true");

        var requestOptions = {
            method: 'GET',
            headers: myHeaders,
            redirect: 'follow'
        };

        var url = '/Users/' + id;
        return this.tsClient.internalFetch(arguments.callee.name, url, requestOptions);
    }

    this.authenticateUser = function authenticateUser(email, userPassword) {
        var myHeaders = new Headers();
        myHeaders.append("Content-Type", "application/json");

        var raw = JSON.stringify({ "email": email, "userPassword": userPassword });

        var requestOptions = {
            method: 'POST',
            headers: myHeaders,
            body: raw,
            redirect: 'follow'
        };

        var url = '/Users/authenticateUser';
        return this.tsClient.internalFetch(arguments.callee.name, url, requestOptions);
    }

    this.registerUser = function registerUser(data) {
        var myHeaders = new Headers();
        myHeaders.append("Content-Type", "application/json");

        var raw = JSON.stringify(data);

        var requestOptions = {
            method: 'POST',
            headers: myHeaders,
            body: raw,
            redirect: 'follow'
        };

        var url = '/Users/registerUser';
        return this.tsClient.internalFetch(arguments.callee.name, url, requestOptions);
    }

    this.logout = function logout() {
        var myHeaders = new Headers();
        myHeaders.append("Authorization", "true");

        var requestOptions = {
            method: 'PUT',
            headers: myHeaders,
            redirect: 'follow'
        };

        var url = '/Users/logout';
        return this.tsClient.internalFetch(arguments.callee.name, url, requestOptions);
    }
}

var ts = new TableServiceApiClient();
document.body.dispatchEvent(new Event('ts_loaded'));


