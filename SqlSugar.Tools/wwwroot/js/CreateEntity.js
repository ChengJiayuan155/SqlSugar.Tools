const { createApp, h } = Vue;

// 分层生成默认输出路径（按用户要求）
const DEFAULT_LAYER_OUTPUT_PATH = 'D:\\\\Users\\\\14440\\\\Desktop';

const app = createApp({
    data() {
        const account = (rule, value, callback) => {
            if (this.SQLServerForm.linkType === 'db') {
                if (value === '') {
                    callback(new Error('请输入数据库用户名'));
                } else {
                    callback();
                }
            } else {
                callback();
            }
        };
        const pwd = (rule, value, callback) => {
            if (this.SQLServerForm.linkType === 'db') {
                if (value === '') {
                    callback(new Error('请输入数据库密码'));
                } else {
                    callback();
                }
            } else {
                callback();
            }
        };

        const oraclePort = (rule, value, callback) => {
            if (this.OracleForm.linkType === 'Basic') {
                if (value === '') {
                    callback(new Error('请输入端口号'));
                } else {
                    callback();
                }
            } else {
                callback();
            }
        };

        const SNSID = (rule, value, callback) => {
            if (this.OracleForm.linkType === 'Basic') {
                if (value === '') {
                    callback(new Error('请输入Service Name/SID'));
                } else {
                    callback();
                }
            } else {
                callback();
            }
        };

        return {
            filterText: '',
            showSettingsDialog: false,
            showLayerDialog: false,
            layerStep: 0,
            testIsSuccess: false,
            loading: false,
            activeIndex: '',
            showSQLServerDialog: false,
            showSQLiteDialog: false,
            SQLServerForm: {
                name: '本地SQLServer',
                host: '127.0.0.1',
                linkType: 'db',
                account: '',
                pwd: '',
                db: ''
            },
            dbList: [],
            SQLServerFormRules: {
                name: [
                    { required: true, message: '请输入连接名称', trigger: 'blur' }
                ],
                host: [
                    { required: true, message: '请输入主机地址', trigger: 'blur' }
                ],
                account: [
                    { validator: account, trigger: 'blur' }
                ],
                pwd: [
                    { validator: pwd, trigger: 'blur' }
                ],
                db: [
                    { required: true, message: '请选择数据库', trigger: 'blur' }
                ]
            },
            dbData: [],
            defaultProps: {
                children: 'children',
                label: 'label'
            },
            thisNodeData: null,
            settingsForm: {
                namespace: '',
                entityNamespace: 'Entitys',
                baseClassName: '',
                classCapsCount: 0,
                propCapsCount: 0,
                propTrim: false,
                propDefault: false,
                sqlSugarPK: false,
                sqlSugarBZL: false,
                getCus: 'return this._属性;',
                setCus: 'this._属性 = -value-;',
                cusAttr: '',
                cusGouZao: '',
                propType: '1'
            },
            layerForm: {
                rootNamespacePrefix: 'ZhiWeiKeJi',
                projectName: 'Demo',
                generateModel: true,
                generateCommon: true,
                generateDal: true,
                generateBll: true,
                generateWebApi: true,
                generateServices: true,
                generateWebApiBase: true,
                generateWebApiExtensions: true,
                generateWebApiProgramSkeleton: false,
                enableSwagger: true,
                enableCors: true,
                enableJwtAuth: true,
                enableGlobalException: true,
                enablePaging: true,
                enableNLog: true,
                generateNLogConfig: true,
                outputPath: '',
                templateKey: 'default'
            },
            layerResult: {
                done: false,
                outputPath: '',
                fileCount: 0,
                message: ''
            },
            createOneParam: {
                node: null,
                data: null
            },
            createOneSuccess: false,
            currentDbInfo: null,

            SQLiteForm: {
                name: '本地SQLite',
                host: '',
                pwd: ''
            },
            SQLiteFormRules: {
                name: [
                    { required: true, message: '请输入连接名称', trigger: 'blur' }
                ],
                host: [
                    { required: true, message: '请输入DB文件地址或选择文件', trigger: 'blur' }
                ]
            },
            showMySqlDialog: false,
            MySqlForm: {
                name: '本地MySql',
                host: '127.0.0.1',
                port: '3306',
                account: '',
                pwd: '',
                db: ''
            },
            MySqlFormRules: {
                name: [
                    { required: true, message: '请输入连接名称', trigger: 'blur' }
                ],
                host: [
                    { required: true, message: '请输入主机地址', trigger: 'blur' }
                ],
                port: [
                    { required: true, message: '请输入端口号', trigger: 'blur' }
                ],
                account: [
                    { required: true, message: '请输入登录帐号', trigger: 'blur' }
                ],
                pwd: [
                    { required: true, message: '请输入登录密码', trigger: 'blur' }
                ]
            },
            showPGSqlDialog: false,
            PGSqlForm: {
                name: '本地PostgreSQL',
                host: '127.0.0.1',
                port: '5432',
                account: '',
                pwd: '',
                db: ''
            },
            PGSqlFormRules: {
                name: [
                    { required: true, message: '请输入连接名称', trigger: 'blur' }
                ],
                host: [
                    { required: true, message: '请输入主机地址', trigger: 'blur' }
                ],
                port: [
                    { required: true, message: '请输入端口号', trigger: 'blur' }
                ],
                account: [
                    { required: true, message: '请输入登录帐号', trigger: 'blur' }
                ],
                pwd: [
                    { required: true, message: '请输入登录密码', trigger: 'blur' }
                ],
                db: [
                    { required: true, message: '请输入要连接的数据库', trigger: 'blur' }
                ]
            },
            showManagerDialog: false,

            showOracleDialog: false,
            OracleForm: {
                name: '本地Oracle',
                host: '127.0.0.1',
                port: '1521',
                linkType: 'Basic',
                account: '',
                pwd: '',
                SNSID: 'ORCL',
                radio: 'Service'
            },
            OracleFormRules: {
                name: [
                    { required: true, message: '请输入连接名称', trigger: 'blur' }
                ],
                host: [
                    { required: true, message: '请输入主机地址', trigger: 'blur' }
                ],
                port: [
                    { validator: oraclePort, trigger: 'blur' }
                ],
                account: [
                    { required: true, message: '请输入数据库用户名', trigger: 'blur' }
                ],
                pwd: [
                    { required: true, message: '请输入数据库密码', trigger: 'blur' }
                ],
                SNSID: [
                    { validator: SNSID, trigger: 'blur' }
                ]
            }
        };
    },
    computed: {
        hasAnyLoadedTables() {
            const list = Array.isArray(this.dbData) ? this.dbData : [];
            return list.some(x => x && Array.isArray(x.children) && x.children.length > 0);
        }
    },
    watch: {
        filterText(val) {
            this.$refs.tree.filter(val);
        }
    },
    methods: {
        ensureCurrentDbFromChecked() {
            if (this.currentDbInfo) return this.currentDbInfo;
            const tables = this.getCheckedTables();
            if (!tables.length) return null;
            const list = Array.isArray(this.dbData) ? this.dbData : [];
            for (let i = 0; i < list.length; i++) {
                const conn = list[i];
                if (!conn || !Array.isArray(conn.children)) continue;
                for (let j = 0; j < tables.length; j++) {
                    if (conn.children.indexOf(tables[j]) >= 0) {
                        this.currentDbInfo = conn;
                        return conn;
                    }
                }
            }
            return null;
        },
        filterNode(value, data) {
            if (!value) return true;
            return data.label.toLowerCase().indexOf(value.toLowerCase()) !== -1;
        },
        handleSelect(key, keyPath) {
            this.activeIndex = key;
            if (key === "1") {
                this.showSQLServerDialog = true;
            } else if (key === "6") {
                this.showSettingsDialog = true;
            } else if (key === "5") {
                this.showSQLiteDialog = true;
            } else if (key === "2") {
                this.showMySqlDialog = true;
            } else if (key === "4") {
                this.showPGSqlDialog = true;
            } else if (key === "7") {
                this.showManagerDialog = true;
            } else if (key === "3") {
                this.showOracleDialog = true;
            }
        },
        SQLServerDialogClosed() {
            this.SQLServerForm.name = '本地SQLServer';
            this.SQLServerForm.host = '127.0.0.1';
            this.SQLServerForm.linkType = 'db';
            this.SQLServerForm.account = '';
            this.SQLServerForm.pwd = '';
            this.SQLServerForm.db = '';
            this.dbList = [];
            this.testIsSuccess = false;
            this.$refs['SQLServerForm'].clearValidate();
        },
        treeNodeClick(data, node) {
            // 默认树渲染，稳定展示文字：一级节点点了加载表，二级节点点了预览代码
            if (!node) return;
            if (node.level === 1) {
                this.currentDbInfo = data;
                this.loadingTables(data);
                return;
            }
            if (node.level > 1) {
                this.createOne(node, data);
            }
        },
        openLayerDialog() {
            this.ensureCurrentDbFromChecked();
            if (!this.currentDbInfo) {
                this.$message({ message: '请先点击左侧连接（一级节点）加载表，或先勾选至少一张表', type: 'warning' });
                return;
            }
            this.layerStep = 0;
            this.layerResult.done = false;
            this.layerResult.outputPath = '';
            this.layerResult.fileCount = 0;
            this.layerResult.message = '';
            this.showLayerDialog = true;
        },
        nextLayerStep() {
            if (this.layerStep === 0) {
                const tables = this.getCheckedTables();
                if (!tables.length) {
                    this.$message({ message: '请先勾选至少一张表', type: 'warning' });
                    return;
                }
            }
            if (this.layerStep === 1) {
                const selectedLayerCount =
                    (this.layerForm.generateModel ? 1 : 0) +
                    (this.layerForm.generateCommon ? 1 : 0) +
                    (this.layerForm.generateDal ? 1 : 0) +
                    (this.layerForm.generateBll ? 1 : 0) +
                    (this.layerForm.generateWebApi ? 1 : 0) +
                    (this.layerForm.generateServices ? 1 : 0) +
                    (this.layerForm.generateWebApiBase ? 1 : 0);
                if (selectedLayerCount === 0) {
                    this.$message({ message: '请至少选择一个生成层', type: 'warning' });
                    return;
                }
                if (!this.layerForm.outputPath || !this.layerForm.outputPath.trim()) {
                    this.$message({ message: '请先选择输出目录', type: 'warning' });
                    return;
                }
                if (!this.layerForm.projectName || !this.layerForm.projectName.trim()) {
                    this.$message({ message: '请填写项目名/模块名（例如：digital_human）', type: 'warning' });
                    return;
                }
            }
            this.layerStep = Math.min(2, this.layerStep + 1);
        },
        prevLayerStep() {
            this.layerStep = Math.max(0, this.layerStep - 1);
        },
        getCheckedTables() {
            const tree = this.$refs.tree;
            if (!tree) return [];
            const checked = tree.getCheckedNodes(false, true) || [];
            return checked.filter(x => !x.children || x.children.length === 0);
        },
        selectAllTables() {
            const tree = this.$refs.tree;
            if (!tree) return;
            // 仅勾选当前连接下的表（二级节点）
            const conn = this.currentDbInfo || (this.dbData || []).find(x => x && Array.isArray(x.children) && x.children.length > 0);
            if (!conn || !Array.isArray(conn.children) || conn.children.length === 0) {
                this.$message({ message: '请先加载表列表', type: 'warning' });
                return;
            }
            this.currentDbInfo = conn;
            tree.setCheckedNodes(conn.children);
        },
        clearTableChecks() {
            const tree = this.$refs.tree;
            if (!tree) return;
            tree.setCheckedNodes([]);
        },
        exportSelectedLayers() {
            const conn = this.ensureCurrentDbFromChecked() || this.currentDbInfo;
            if (!conn) return;
            const tables = this.getCheckedTables();
            if (!tables.length) {
                this.$message({ message: '请勾选至少一张表', type: 'warning' });
                return;
            }
            if (!this.layerForm.outputPath || !this.layerForm.outputPath.trim()) {
                this.$message({ message: '请先选择输出目录', type: 'warning' });
                return;
            }
            this.loading = true;
            const info = JSON.stringify({
                linkString: conn.linkString,
                settings: JSON.stringify(this.settingsForm),
                layer: JSON.stringify(this.layerForm),
                tableList: JSON.stringify(tables)
            });
            if (conn.type === 'sqlserver') {
                sqlServer.generateLayers(info);
            } else if (conn.type === 'sqlite') {
                sqlite.generateLayers(info);
            } else if (conn.type === 'mysql') {
                mysql.generateLayers(info);
            } else if (conn.type === 'pgsql') {
                pgsql.generateLayers(info);
            } else if (conn.type === 'oracle') {
                oracle.generateLayers(info);
            } else {
                this.loading = false;
            }
        },
        selectLayerOutputFolder() {
            const init = (this.layerForm.outputPath || '').toString().trim();
            selectFolder(init);
        },
        testSQLServerLink() {
            this.$refs['SQLServerForm'].validate((valid) => {
                if (valid) {
                    this.loading = true;
                    if (this.SQLServerForm.linkType === 'win') {
                        sqlServer.testLink(`Data Source=${this.SQLServerForm.host};Initial Catalog=master;Integrated Security=True`);
                    } else {
                        sqlServer.testLink(`Data Source=${this.SQLServerForm.host};Initial Catalog=master;Persist Security Info=True;User ID=${this.SQLServerForm.account};Password=${this.SQLServerForm.pwd}`);
                    }
                }
            });
        },
        openOutputFolder() {
            if (!this.layerResult.outputPath) {
                this.$message({ message: '暂无可打开的输出目录', type: 'warning' });
                return;
            }
            openFolder(this.layerResult.outputPath);
        },
        selectDB() {
            if (!this.testIsSuccess) {
                this.$message({
                    message: '请先测试连接',
                    type: 'warning'
                });
                return;
            }
            this.$refs['SQLServerForm'].validate((valid) => {
                if (valid) {
                    if (this.SQLServerForm.db === '') {
                        this.$message({
                            message: '请选择一个数据库',
                            type: 'warning'
                        });
                        return;
                    }
                    let linkString = '';
                    if (this.SQLServerForm.linkType === 'win') {
                        linkString = `Data Source=${this.SQLServerForm.host};Initial Catalog=${this.SQLServerForm.db};Integrated Security=True`;
                    } else {
                        linkString = `Data Source=${this.SQLServerForm.host};Initial Catalog=${this.SQLServerForm.db};Persist Security Info=True;User ID=${this.SQLServerForm.account};Password=${this.SQLServerForm.pwd}`;
                    }
                    const dbInfo = {
                        label: this.SQLServerForm.name,
                        linkString,
                        children: [],
                        type: 'sqlserver',
                        host: this.SQLServerForm.host,
                        linkType: this.SQLServerForm.linkType,
                        account: this.SQLServerForm.account,
                        pwd: this.SQLServerForm.pwd,
                        db: this.SQLServerForm.db
                    };
                    this.dbData.push(dbInfo);
                    this.showSQLServerDialog = false;
                    addedDBData(dbInfo);
                }
            });
        },
        loadingTables(dbInfo) {
            this.loading = true;
            this.thisNodeData = dbInfo;
            if (dbInfo.type === 'sqlserver') {
                sqlServer.loadingTables(dbInfo.linkString);
            } else if (dbInfo.type === 'sqlite') {
                sqlite.loadingTables(dbInfo.linkString);
            } else if (dbInfo.type === 'mysql') {
                mysql.loadingTables(dbInfo.linkString);
            } else if (dbInfo.type === 'pgsql') {
                pgsql.loadingTables(dbInfo.linkString);
            } else if (dbInfo.type === 'oracle') {
                oracle.loadingTables(dbInfo.linkString);
            }
        },
        settingsSave() {
            const settingsJson = JSON.stringify(this.settingsForm);
            window.localStorage.setItem("settingsJson", settingsJson);
            this.showSettingsDialog = false;
        },
        createOne(node, data) {
            this.loading = true;
            this.createOneParam.node = node;
            this.createOneParam.data = data;
            const linkString = node.parent.data.linkString;
            const tableDesc = data.TableDesc;
            const tableName = data.label;
            const info = JSON.stringify({
                linkString,
                tableDesc,
                tableName,
                settings: JSON.stringify(this.settingsForm)
            });
            if (node.parent.data.type === 'sqlserver') {
                sqlServer.createOne(info);
            } else if (node.parent.data.type === 'sqlite') {
                sqlite.createOne(info);
            } else if (node.parent.data.type === 'mysql') {
                mysql.createOne(info);
            } else if (node.parent.data.type === 'pgsql') {
                pgsql.createOne(info);
            } else if (node.parent.data.type === 'oracle') {
                oracle.createOne(info);
            }
        },
        saveOneCode() {
            if (this.createOneSuccess && this.createOneParam.node !== null && this.createOneParam.data !== null) {
                this.loading = true;
                const linkString = this.createOneParam.node.parent.data.linkString;
                const tableDesc = this.createOneParam.data.TableDesc;
                const tableName = this.createOneParam.data.label;
                const info = JSON.stringify({
                    linkString,
                    tableDesc,
                    tableName,
                    settings: JSON.stringify(this.settingsForm)
                });
                if (this.createOneParam.node.parent.data.type === 'sqlserver') {
                    sqlServer.saveOne(info);
                } else if (this.createOneParam.node.parent.data.type === 'sqlite') {
                    sqlite.saveOne(info);
                } else if (this.createOneParam.node.parent.data.type === 'mysql') {
                    mysql.saveOne(info);
                } else if (this.createOneParam.node.parent.data.type === 'pgsql') {
                    pgsql.saveOne(info);
                } else if (this.createOneParam.node.parent.data.type === 'oracle') {
                    oracle.saveOne(info);
                }
            }
        },
        saveAllTables(node, data) {
            if (data.children.length <= 0) {
                this.$message({
                    message: '该数据库没有表或您还没有加载表列表',
                    type: 'warning'
                });
                return;
            }
            this.loading = true;
            const linkString = node.data.linkString;
            const info = JSON.stringify({
                linkString,
                settings: JSON.stringify(this.settingsForm),
                tableList: JSON.stringify(data.children)
            });
            if (node.data.type === 'sqlserver') {
                sqlServer.saveAllTables(info);
            } else if (node.data.type === 'sqlite') {
                sqlite.saveAllTables(info);
            } else if (node.data.type === 'mysql') {
                mysql.saveAllTables(info);
            } else if (node.data.type === 'pgsql') {
                pgsql.saveAllTables(info);
            } else if (node.data.type === 'oracle') {
                oracle.saveAllTables(info);
            }
        },
        SQLiteDialogClosed() {
            this.SQLiteForm.name = '本地SQLite';
            this.SQLiteForm.host = '';
            this.SQLiteForm.pwd = '';
            this.testIsSuccess = false;
            this.$refs['SQLiteForm'].clearValidate();
        },
        selectDBFile() {
            sqlite.selectDBFile();
        },
        testSQLiteLink() {
            this.$refs['SQLiteForm'].validate((valid) => {
                if (valid) {
                    this.loading = true;
                    if (this.SQLiteForm.pwd !== '') {
                        sqlite.testLink(`Data Source=${this.SQLiteForm.host};Password=${this.SQLiteForm.pwd};`);
                    } else {
                        sqlite.testLink(`Data Source=${this.SQLiteForm.host};`);
                    }
                }
            });
        },
        selectSQLiteDB() {
            if (!this.testIsSuccess) {
                this.$message({
                    message: '请先测试连接',
                    type: 'warning'
                });
                return;
            }
            this.$refs['SQLiteForm'].validate((valid) => {
                if (valid) {
                    let linkString = '';
                    if (this.SQLiteForm.pwd !== '') {
                        linkString = `Data Source=${this.SQLiteForm.host};Password=${this.SQLiteForm.pwd};`;
                    } else {
                        linkString = `Data Source=${this.SQLiteForm.host};`;
                    }
                    const dbInfo = {
                        label: this.SQLiteForm.name,
                        linkString,
                        children: [],
                        type: 'sqlite',
                        host: this.SQLiteForm.host,
                        pwd: this.SQLiteForm.pwd
                    };
                    this.dbData.push(dbInfo);
                    this.showSQLiteDialog = false;
                    addedDBData(dbInfo);
                }
            });
        },
        mySqlDialogClosed() {
            this.MySqlForm.name = '本地MySql';
            this.MySqlForm.host = '127.0.0.1';
            this.MySqlForm.port = '3306';
            this.MySqlForm.account = '';
            this.MySqlForm.pwd = '';
            this.MySqlForm.db = '';
            this.dbList = [];
            this.testIsSuccess = false;
            this.$refs['MySqlForm'].clearValidate();
        },
        testMySqlLink() {
            this.$refs['MySqlForm'].validate((valid) => {
                if (valid) {
                    this.loading = true;
                    let linkString = `server=${this.MySqlForm.host};User Id=${this.MySqlForm.account};password=${this.MySqlForm.pwd};port=${this.MySqlForm.port};`;
                    mysql.testLink(linkString);
                }
            });
        },
        selectMySqlDB() {
            if (!this.testIsSuccess) {
                this.$message({
                    message: '请先测试连接',
                    type: 'warning'
                });
                return;
            }
            this.$refs['MySqlForm'].validate((valid) => {
                if (valid) {
                    if (this.MySqlForm.db === '') {
                        this.$message({
                            message: '请选择一个数据库',
                            type: 'warning'
                        });
                        return;
                    }
                    let linkString = `server=${this.MySqlForm.host};User Id=${this.MySqlForm.account};password=${this.MySqlForm.pwd};Database=${this.MySqlForm.db};port=${this.MySqlForm.port};`;
                    const dbInfo = {
                        label: this.MySqlForm.name,
                        linkString,
                        children: [],
                        type: 'mysql',
                        host: this.MySqlForm.host,
                        port: this.MySqlForm.port,
                        account: this.MySqlForm.account,
                        pwd: this.MySqlForm.pwd,
                        db: this.MySqlForm.db
                    };
                    this.dbData.push(dbInfo);
                    this.showMySqlDialog = false;
                    addedDBData(dbInfo);
                }
            });
        },
        PGSqlDialogClosed() {
            this.PGSqlForm.name = '本地PostgreSQL';
            this.PGSqlForm.host = '127.0.0.1';
            this.PGSqlForm.port = '5432';
            this.PGSqlForm.account = '';
            this.PGSqlForm.pwd = '';
            this.PGSqlForm.db = '';
            this.dbList = [];
            this.testIsSuccess = false;
            this.$refs['PGSqlForm'].clearValidate();
        },
        testPGSqlLink() {
            this.$refs['PGSqlForm'].validate((valid) => {
                if (valid) {
                    this.loading = true;
                    let linkString = `Host=${this.PGSqlForm.host};Port=${this.PGSqlForm.port};Username=${this.PGSqlForm.account};Password=${this.PGSqlForm.pwd};Database=${this.PGSqlForm.db};`;
                    pgsql.testLink(linkString);
                }
            });
        },
        selectPGSqlDB() {
            if (!this.testIsSuccess) {
                this.$message({
                    message: '请先测试连接',
                    type: 'warning'
                });
                return;
            }
            this.$refs['PGSqlForm'].validate((valid) => {
                if (valid) {
                    let linkString = `Host=${this.PGSqlForm.host};Port=${this.PGSqlForm.port};Username=${this.PGSqlForm.account};Password=${this.PGSqlForm.pwd};Database=${this.PGSqlForm.db};`;
                    const dbInfo = {
                        label: this.PGSqlForm.name,
                        linkString,
                        children: [],
                        type: 'pgsql',
                        host: this.PGSqlForm.host,
                        port: this.PGSqlForm.port,
                        account: this.PGSqlForm.account,
                        pwd: this.PGSqlForm.pwd,
                        db: this.PGSqlForm.db
                    };
                    this.dbData.push(dbInfo);
                    this.showPGSqlDialog = false;
                    addedDBData(dbInfo);
                }
            });
        },
        deleteDB(index, name) {
            this.$confirm(`确定删除名为: [${name}] 的数据库连接吗?`, '删除提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning'
            }).then(() => {
                deleteDBData(index);
                this.dbData.splice(index, 1);
                this.$message({
                    type: 'success',
                    message: '删除成功!'
                });
            }).catch(() => {
            });
        },
        editDB(index, name) {
            this.$prompt('请输入新的连接名称', `修改 [${name}] 的连接名称`, {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                inputPlaceholder: '请输入新的连接名称哦~',
                inputValidator: (val) => {
                    if (!val) {
                        return '请输入连接名称';
                    }
                    return true;
                }
            }).then(({ value }) => {
                let dbData = getDBData();
                dbData[index].label = value;
                window.localStorage.setItem('dbDataKey', JSON.stringify(dbData));
                this.dbData[index].label = value;
                this.$message({
                    type: 'success',
                    message: '编辑成功!'
                });
            }).catch(() => {
            });
        },
        OracleDialogClosed() {
            this.$refs['OracleForm'].resetFields();
            this.OracleForm.radio = 'Service';
            this.OracleForm.linkType = 'Basic';
            this.OracleForm.host = '127.0.0.1';
            this.OracleForm.name = '本地Oracle';
        },
        testOracleLink() {
            if (this.OracleForm.linkType === 'TNS') {
                this.$message({
                    message: '暂时不支持TNS连接',
                    type: 'warning'
                });
                return;
            }
            this.$refs['OracleForm'].validate((valid) => {
                if (valid) {
                    this.loading = true;
                    let itemService = '';
                    if (this.OracleForm.radio === 'Service') {
                        itemService = 'SERVICE_NAME';
                    } else {
                        itemService = 'SID';
                    }
                    let linkString = `Password=${this.OracleForm.pwd};User ID=${this.OracleForm.account};Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=${this.OracleForm.host})(PORT=${this.OracleForm.port})))(CONNECT_DATA=(SERVER=DEDICATED)(${itemService}=${this.OracleForm.SNSID})));`;
                    oracle.testLink(linkString);
                }
            });
        },
        selectOracleDB() {
            if (!this.testIsSuccess) {
                this.$message({
                    message: '请先测试连接',
                    type: 'warning'
                });
                return;
            }
            this.$refs['OracleForm'].validate((valid) => {
                if (valid) {
                    let itemService = '';
                    if (this.OracleForm.radio === 'Service') {
                        itemService = 'SERVICE_NAME';
                    } else {
                        itemService = 'SID';
                    }
                    let linkString = `Password=${this.OracleForm.pwd};User ID=${this.OracleForm.account};Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=${this.OracleForm.host})(PORT=${this.OracleForm.port})))(CONNECT_DATA=(SERVER=DEDICATED)(${itemService}=${this.OracleForm.SNSID})));`;
                    this.showOracleDialog = false;
                    const dbInfo = {
                        label: this.OracleForm.name,
                        linkString,
                        children: [],
                        type: 'oracle',
                        host: this.OracleForm.host,
                        port: this.OracleForm.port,
                        linkType: this.OracleForm.linkType,
                        account: this.OracleForm.account,
                        pwd: this.OracleForm.pwd,
                        SNSID: this.OracleForm.SNSID,
                        radio: this.OracleForm.radio
                    };
                    this.dbData.push(dbInfo);
                    addedDBData(dbInfo);
                }
            });
        }
    },
    created() {
        this.dbData = normalizeDbData(getDBData());
        const lastOut = window.localStorage.getItem('layerOutputPath');
        this.layerForm.outputPath = (lastOut && lastOut.trim()) ? lastOut.trim() : DEFAULT_LAYER_OUTPUT_PATH;
        const settingsJson = window.localStorage.getItem("settingsJson");
        if (settingsJson !== undefined && settingsJson !== null && settingsJson !== "") {
            const settingsObject = JSON.parse(settingsJson);
            this.settingsForm.namespace = settingsObject.namespace;
            this.settingsForm.entityNamespace = settingsObject.entityNamespace;
            this.settingsForm.baseClassName = settingsObject.baseClassName;
            this.settingsForm.classCapsCount = settingsObject.classCapsCount;
            this.settingsForm.propCapsCount = settingsObject.propCapsCount;
            this.settingsForm.propTrim = settingsObject.propTrim;
            this.settingsForm.propDefault = settingsObject.propDefault;
            this.settingsForm.sqlSugarPK = settingsObject.sqlSugarPK;
            this.settingsForm.sqlSugarBZL = settingsObject.sqlSugarBZL;
            this.settingsForm.getCus = settingsObject.getCus;
            this.settingsForm.setCus = settingsObject.setCus;
            this.settingsForm.cusAttr = settingsObject.cusAttr;
            this.settingsForm.cusGouZao = settingsObject.cusGouZao;
            this.settingsForm.propType = settingsObject.propType;
        }
    }
});

app.use(ElementPlus);
app.config.globalProperties.$message = ElementPlus.ElMessage;
app.config.globalProperties.$confirm = ElementPlus.ElMessageBox.confirm;
app.config.globalProperties.$prompt = ElementPlus.ElMessageBox.prompt;
const vue = app.mount('#app');

function addedDBData(dbInfo) {
    let dbData = getDBData();
    dbData.push(dbInfo);
    window.localStorage.setItem('dbDataKey', JSON.stringify(dbData));
}

function deleteDBData(index) {
    let dbData = getDBData();
    dbData.splice(index, 1);
    window.localStorage.setItem('dbDataKey', JSON.stringify(dbData));
}

function getDBData() {
    let json = localStorage.getItem('dbDataKey');
    if (json === undefined || json === null || json === "") {
        return [];
    }
    return JSON.parse(json);
}

function normalizeDbData(list) {
    if (!Array.isArray(list)) return [];
    return list.map((item, idx) => {
        const x = item || {};
        const label = (x.label || '').toString().trim() || `连接${idx + 1}`;
        const children = Array.isArray(x.children) ? x.children : [];
        return Object.assign({}, x, { label, children });
    });
}

function testSuccessMsg() {
    vue.testIsSuccess = true;
    vue.$message({
        message: '测试连接成功, 正在读取数据库信息...',
        type: 'success'
    });
}

function hideLoading() {
    vue.loading = false;
}

function setDbList(json) {
    const dbList = JSON.parse(json);
    vue.dbList = dbList;
}

function setTables(json) {
    vue.dbData[vue.dbData.indexOf(vue.thisNodeData)].children = JSON.parse(json);
}

function getEntityCode(code) {
    document.getElementById("code").innerHTML = code;
    vue.createOneSuccess = true;
}

function saveOneSuccess() {
    vue.$message({
        message: '保存成功',
        type: 'success'
    });
}

function saveAllTablesSuccess() {
    vue.$message({
        message: '导出所有表成功',
        type: 'success'
    });
}

function generateLayersSuccess(msg) {
    let result = {};
    try {
        result = msg ? JSON.parse(msg) : {};
    } catch (e) {
        result = { message: msg || '' };
    }
    vue.showLayerDialog = false;
    vue.layerResult.done = true;
    vue.layerResult.outputPath = result.outputPath || '';
    vue.layerResult.fileCount = result.fileCount || 0;
    vue.layerResult.message = result.message || '分层生成成功';
    vue.$message({
        message: vue.layerResult.message,
        type: 'success'
    });
    if (vue.layerResult.outputPath) {
        window.localStorage.setItem('layerOutputPath', vue.layerResult.outputPath);
        vue.layerForm.outputPath = vue.layerResult.outputPath;
    }
}

function setLayerOutputPath(path) {
    const v = (path || '').toString().trim();
    if (v) {
        vue.layerForm.outputPath = v;
        window.localStorage.setItem('layerOutputPath', v);
    }
}

function setSQLiteFilePath(path) {
    vue.SQLiteForm.host = path;
}