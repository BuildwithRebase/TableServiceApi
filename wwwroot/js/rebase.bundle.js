'use strict';

/**
 * Utility functions
 */
function splitRebaseData(rebaseData) {
    const parts = rebaseData.split('.');
    return (parts.length == 3) ?
        { entity: parts[0], mode: parts[1], type: parts[2] } :
        { entity: parts[0], type: parts[1] }
}

function assignHandler(selector, handler, cb) {
    return (handler) ? handler : $(selector).click(cb);
}

function processTemplate(template, data) {
    return template(data);
}

function processRebaseTemplate(rebaseTemplates, resource, data) {
    if (!rebaseTemplates[resource]) return;

    const output = processTemplate(rebaseTemplates[resource].template, data);
    rebaseTemplates[resource].container.innerHTML = output;
}

function formToData(form) {
    const fields = $(form).find('[data-rebase-field]');
    const data = {};
    _.forEach(fields, function (field) {
        data[field.dataset.rebaseField] = field.value;
    })
    return data;
}

/**
 * TsClient for making Axios data calls
 */
function TsClient(alertCb = null) {
    this.baseUrl = (window.location.href.indexOf('localhost') > -1) ? 'https://localhost:5001/api' : 'https://appcore.buildwithrebase.online/api';
    this.alertCb = alertCb;
}

TsClient.prototype.createUrl = function createUrl(resource, page, pageSize) {
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

TsClient.prototype.getEntities = async function getEntities(resource, page, pageSize) {
    try {
        this.clearAlert();
        const url = this.baseUrl + this.createUrl(resource, page, pageSize);
        const { data } = await axios.get(url);
        return data;
    } catch (ex) {
        this.addAlert({ type: 'Error', message: ex });
        return null;
    }
}

TsClient.prototype.getEntity = async function getEntity(resource, id) {
    try {
        this.clearAlert();
        const url = this.baseUrl + '/' + resource + '/' + id;
        const { data } = await axios.get(url);
        return data;
    } catch (ex) {
        this.addAlert({ type: 'Error', message: ex });
        return null;
    }
}

TsClient.prototype.getData = async function getData(resource) {
    try {
        this.clearAlert();
        const url = this.baseUrl + '/' + resource;
        const { data } = await axios.get(url);
        return data;
    } catch (ex) {
        this.addAlert({ type: 'Error', message: ex });
        return null;
    }
}

TsClient.prototype.postData = async function postData(resource, dataToPost) {
    try {
        this.clearAlert();
        const url = this.baseUrl + '/' + resource;
        if (dataToPost) {
            const { data } = await axios.post(url, dataToPost);
            return data;
        } else {
            const { data } = await axios.post(url);
            return data;
        }
    } catch (ex) {
        this.addAlert({ type: 'Error', message: ex });
        return null;
    }
}

TsClient.prototype.putData = async function putData(resource, id, dataToUpdate) {
    try {
        this.clearAlert();
        const url = this.baseUrl + '/' + resource + '/' + id;
        const { data } = await axios.put(url, dataToUpdate);
        return data;
    } catch (ex) {
        this.addAlert({ type: 'Error', message: ex });
        return null;
    }
}

TsClient.prototype.deleteData = async function deleteData(resource, id) {
    try {
        this.clearAlert();
        const url = this.baseUrl + '/' + resource + '/' + id;
        const { data } = await axios.delete(url);
        return data;
    } catch (ex) {
        this.addAlert({ type: 'Error', message: ex });
        return null;
    }
}
TsClient.prototype.registerUser = async function registerUser(data) {
    try {
        this.clearAlert();
        var url = this.baseUrl + '/Users/registerUser';
        const { d } = await axios.post(url, data);
        return d;
    } catch (ex) {
        this.addAlert({ type: 'Error', message: ex });
        return null;
    }
}

TsClient.prototype.clearAlert = function clearAlert() {
    if (!this.alertCb) {
        return;
    }
    alertCb(null);
}

TsClient.prototype.addAlert = function addAlert(alert) {
    if (!this.alertCb) {
        return;
    }
    alertCb(alert);
}

/**
 * Ts Services used to provide a business layer around making API Calls
 */
function TsServiceBase(tsClient) {
    if (tsClient == null) throw 'tsClient is required for a Ts*Service'
    this.tsClient = tsClient;
}

/**
 * TsUsersService used for authentication, registering and logging out
 */
function TsUsersService(tsClient) {
    TsServiceBase.call(this, tsClient);
}
TsUsersService.prototype = Object.create(TsServiceBase.prototype);
Object.defineProperty(TsUsersService.prototype, 'constructor', {
    value: TsUsersService,
    enumerable: false, // so that it does not appear in 'for in' loop
    writable: true
});

TsUsersService.prototype.authenticateUser = async function authenticateUser({ email, userPassword }) {
    const data = await this.tsClient.postData('Users/authenticateUser', { email, userPassword });
    if (data != null) {
        axios.defaults.headers.common['Authorization'] = `Bearer ${data.token}`
    }
    return data;
}

TsUsersService.prototype.registerUser = async function registerUser(data) {
    return await this.tsClient.postData('Users/registerUser', data);
}
TsUsersService.prototype.getHMAC = async function getHMAC() {
    return await this.tsClient.getData('Account/getBillFlowHMAC');
}


TsUsersService.logout = async function logout() {
    const url = this.tsClient.baseUrl + '/Users/logout';
    const { data } = await axios.put(url);
    axios.defaults.headers.common['Authorization'] = null;
    return data;
}

/**
 * TsDataService will make the connection to the custom table
 */
function TsDataService(tsClient) {
    TsServiceBase.call(this, tsClient);
}
TsDataService.prototype = Object.create(TsServiceBase.prototype);
Object.defineProperty(TsDataService.prototype, 'constructor', {
    value: TsDataService,
    enumerable: false, // so that it does not appear in 'for in' loop
    writable: true
});

TsDataService.prototype.getTeamEntities = async function getTeamEntities() {
    return await this.tsClient.getData('Data');
}

TsDataService.prototype.getTableDataItems = async function getTableDataItems(tableName, page, pageSize) {
    return await this.tsClient.getEntities('Data/' + tableName, page, pageSize);
}

TsDataService.prototype.addTableDataItem = async function addTableDataItem(tableName, dataItem) {
    return await this.tsClient.postData('Data/' + tableName, dataItem);
}

TsDataService.prototype.postTableDataItem = async function postTableDataItem(tableName, id, action, dataItem) {
    return await this.tsClient.postData('Data/' + tableName + '/' + id + '/' + action, dataItem);
}

TsDataService.prototype.getTableDataItem = async function getTableDataItem(tableName, id) {
    return await this.tsClient.getEntity('Data/' + tableName, id);
}

TsDataService.prototype.updateTableDataItem = async function updateTableDataItem(tableName, id, dataItem) {
    return await this.tsClient.putData('Data/' + tableName, id, dataItem);
}

TsDataService.prototype.deleteTableDataItem = async function deleteTableDataItem(tableName, id) {
    return await this.tsClient.deleteData('Data/' + tableName, id);
}

/**
 * Base class for rebase controls
 */
function RebaseControl(parentControl) {
    this.parentControl = parentControl
}

RebaseControl.prototype.processTemplate = function (data) {
    this.container.innerHTML = this.template(data);
}

RebaseControl.prototype.assignTemplate = function (tmpl, text, resource) {
    this.resource = resource;
    this.container = $(tmpl).parent()[0];
    this.text = text;
    this.template = Handlebars.compile(text);
}

RebaseControl.prototype.assignFields = function (tmpl) {
    var fields = [];
    $(tmpl).find('.rebase-control').each(function (index, rc) {
        fields.push(rc.id);
    });
}

RebaseControl.prototype.removeOriginalTemplate = function (tmpl) {
    this.container.removeChild(tmpl);
}

/**
 * Represents a rebase table
 *
 * <div class="rebase-control rebase-control-table"></div>
 */
function RebaseControlTable(parentControl) {
    RebaseControl.call(this, parentControl);

    var rowTmpl = $(parentControl).find('[data-rebase-item]')[0];
    var table = $(rowTmpl).data('rebaseItem').split('.')[0];
    this.rebasePagingId = $(rowTmpl).data('rebasePaging');
    var text = "{{#each " + table + "}}" + $(rowTmpl).parent()[0].innerHTML + "{{/each}}";
    this.assignTemplate(rowTmpl, text, table);
    this.removeOriginalTemplate(rowTmpl);

    // see if there are any buttons
    const actions = $(parentControl).find('[data-rebase-action]');
    _.forEach(actions, function (action) {
        action.type = 'button';
    });

}

RebaseControlTable.prototype = Object.create(RebaseControl.prototype);
Object.defineProperty(RebaseControlTable.prototype, 'constructor', {
    value: RebaseControlTable,
    enumerable: false, // so that it does not appear in 'for in' loop
    writable: true
});

/** 
 * Represents a rebase paging area
 */
function RebaseControlPaging(parentControl) {
    RebaseControl.call(parentControl);

    const self = this;

    const pagers = $(parentControl).find('[data-rebase-paging]');
    _.forEach(pagers, function (pager) {

        const tmpl = pager;
        const { entity, mode, type } = splitRebaseData(pager.dataset.rebasePaging);
        const text = $(parentControl)[0].innerHTML;
        const resource = entity + type + 'Paging';
        self.assignTemplate(tmpl, text, resource);
        self.removeOriginalTemplate(tmpl);

    });

}

RebaseControlPaging.prototype = Object.create(RebaseControl.prototype);
Object.defineProperty(RebaseControlPaging.prototype, 'constructor', {
    value: RebaseControlPaging,
    enumerable: false, // so that it does not appear in 'for in' loop
    writable: true
});

/**
 * Represents a rebase form
 *
 * <div class="rebase-control rebase-control-form"></div>
 */
function RebaseControlForm(parentControl) {
    RebaseControl.call(this, parentControl);

    var formTmpl = $(parentControl).find('[data-rebase-item]')[0];
    var form = $(formTmpl).data('rebaseItem').split('.')[0];
    this.assignFields(formTmpl);
    this.assignTemplate(formTmpl, formTmpl.innerHTML, form + 'Form');
    this.removeOriginalTemplate(formTmpl);

}

RebaseControlForm.prototype = Object.create(RebaseControl.prototype);
Object.defineProperty(RebaseControlForm.prototype, 'constructor', {
    value: RebaseControlForm,
    enumerable: false, // so that it does not appear in 'for in' loop
    writable: true
});

/**
 * Provides the business logic for the Rebase Component
 */
function RebaseComponent(tsClient) {
    this.rebaseTemplates = {};
    this.model = {};
    this.version = '1.0';
    this.resource = '';
    this.mode = '';
    this.tsDataService = new TsDataService(tsClient);
    this.tsUsersService = new TsUsersService(tsClient);
    this.isLoggedOn = false;
    this.handlers = { rebaseLogin: null, rebase: null }
    this.paging = { page: 1 }
}

RebaseComponent.prototype.initialise = async function initialise() {
    var _this = this;
    console.log('Loading rebase');
    var self = this;
    if (Object.keys(this.rebaseTemplates).length == 0) {
        this.loadTemplates();
    }
    this.handlers.rebaseLogin = assignHandler('#rebaseLogin', this.handlers.rebaseLogin, function (ev) {
        if (ev.target.dataset.rebaseAction === 'login') {
            self.login(self);
        }
    });
    this.handlers.rebase = assignHandler('#rebase', this.handlers.rebase, function (ev) {
        if (ev.target.dataset.rebaseAction) {
            const { entity, mode, type } = splitRebaseData(ev.target.dataset.rebaseAction);
            if (type === 'new') {
                self.addDataForAction(ev.target, entity);
            } else if (type === 'edit') {
                self.editData(ev.target);
            } else if (type === 'save') {
                self.saveDataForAction(ev.target, entity);
            } else if (type === 'delete') {
                self.deleteDataForAction(ev.target, entity);
            } else if (type === 'prev') {
                self.prevPage();
            } else if (type === 'next') {
                self.nextPage();
            }
        }
    });
}

RebaseComponent.prototype.signout = async function signout() {
    await this.tsUsersService.logout();
    this.isLoggedOn = false;
    document.getElementById('rebase').style.display = 'none';
    document.getElementById('rebaseLogin').style.display = 'block';
}

RebaseComponent.prototype.loadTemplates = function loadTemplates() {

    console.log('loadTemplates');
    const self = this;

    const rebaseControls = $('#rebase').find('.rebase-control');
    _.forEach(rebaseControls, function (rebaseControl) {
        if ($(rebaseControl).hasClass('rebase-control-table')) {
            const rebaseTable = new RebaseControlTable(rebaseControl);
            self.rebaseTemplates[rebaseTable.resource] = rebaseTable;
            self.resource = rebaseTable.resource;
        } else if ($(rebaseControl).hasClass('rebase-control-form')) {
            const rebaseForm = new RebaseControlForm(rebaseControl);
            self.rebaseTemplates[rebaseForm.resource] = rebaseForm;
        } else if ($(rebaseControl).hasClass('rebase-control-paging')) {
            const rebasePaging = new RebaseControlPaging(rebaseControl);
            self.rebaseTemplates[rebasePaging.resource] = rebasePaging;
        } // ... etc
    });

    console.log(this.rebaseTemplates);
}

RebaseComponent.prototype.getData = async function getData(page) {
    if (page === void 0) { page = 1; }
    console.log('getData');
    const response = await this.tsDataService.getTableDataItems(this.resource, page, 10);

    this.model[this.resource] = response.data;
    this.paging.page = response.page;
    this.paging.pageSize = response.pageSize;
    this.paging.pages = response.pages;
    this.paging.recordEnd = response.recordEnd;
    this.paging.recordStart = response.recordStart;
    this.paging.totalCount = response.totalCount;

    if (Object.keys(this.rebaseTemplates)) {
        processRebaseTemplate(this.rebaseTemplates, this.resource, this.model);
        processRebaseTemplate(this.rebaseTemplates, this.resource + 'topPaging', this.paging);
        processRebaseTemplate(this.rebaseTemplates, this.resource + 'bottomPaging', this.paging);
    }
}

RebaseComponent.prototype.prevPage = function () {
    if (this.paging.page == 1) return;
    this.getData(this.paging.page - 1);
}

RebaseComponent.prototype.nextPage = function () {
    if (this.paging.page >= this.paging.pages) return;
    this.getData(this.paging.page + 1);
}

RebaseComponent.prototype.editData = async function editData(el) {
    const id = $(el).data('rebaseId');

    const data = await this.tsDataService.getTableDataItem(this.resource, id);

    var form = this.resource + 'Form';
    var text = this.rebaseTemplates[form].template(data);

    this.rebaseTemplates[form].container.innerHTML = text;

    this.mode = 'edit';

    // to-do create a better way of doing this :)
    $('#showTasksEditModal').click();

}

RebaseComponent.prototype.deleteRecord = function deleteRecord(el) {
    this.deleteId = $(el).data('id');
    this.mode = 'delete';
}

RebaseComponent.prototype.addDataForAction = async function addDataForAction(control, entity) {
    const data = formToData(control.form);
    // to-do: add a form validation routine
    await this.tsDataService.addTableDataItem(entity, data);
    this.getData(this.paging.page);
}

RebaseComponent.prototype.saveDataForAction = async function saveDataForAction(control, entity) {
    const data = formToData(control.form);
    // to-do: add a form validation route;
    data.Id = data.Id ? parseInt(data.Id) : 0;
    await this.tsDataService.updateTableDataItem(entity, data.Id, data);
    this.getData(this.paging.page);
}

RebaseComponent.prototype.deleteDataForAction = async function deleteDataForAction(control, entity) {
    const id = $(control).data('rebaseId');
    await this.tsDataService.deleteTableDataItem(entity, id);
    this.getData(this.paging.page);
}

RebaseComponent.prototype.createData = function createData(self) {
    console.log('createData');
    var data = {};
    var form = self.resource + 'Form';
    self.resource.substring(0, self.resource.length - 1) + 'Id';
    $(self.rebaseTemplates[form].fields).each(function (index, value) {
        data[value] = '';
    });
    var text = self.rebaseTemplates[form].template(data);
    self.rebaseTemplates[form].container.innerHTML = text;
    self.mode = 'save';
}

RebaseComponent.prototype.saveData = async function saveData(self) {
    console.log('saveData');
    data = {};
    form = self.resource + 'Form';
    $(self.rebaseTemplates[form].container).find('.rebase-control').each(function (index, rebaseControl) {
        data[rebaseControl.id] = rebaseControl.value;
    });
    (self.mode === 'edit') ? 'POST' : 'PUT';
    if (self.mode === 'edit') {
        await this.tsDataService.updateTableDataItem(this.resource, data['id'], data);
    } else {
        await this.tsDataService.addTableDataItem(this.resource, data);
    }
}

RebaseComponent.prototype.deleteData = async function deleteData(self) {
    console.log('deleteData');
    await this.tsDataService.deleteTableDataItem(this.resource, self.deleteId);
}

RebaseComponent.prototype.login = async function login(self) {
    var email, password;
    email = $('[data-rebase-field="email"]').val();
    password = $('[data-rebase-field="password"]').val();

    const response = await this.tsUsersService.authenticateUser({ email: email, userPassword: password });

    if (response != null) {
        document.getElementById('rebase').style.display = 'block';
        document.getElementById('rebaseLogin').style.display = 'none';

        this.getData();
    }
}

const rebaseComponent = new RebaseComponent(new TsClient());
window['rebaseComponent'] = rebaseComponent;

(function () {
    document.getElementById('rebase').style.display = 'none';
    document.getElementById('rebaseLogin').style.display = 'block';

    setTimeout(function () {
        var pageController = function () {
            rebaseComponent.initialise();
        };
        pageController();
    }, 1000);

})();
