const API_BASE = '/api';
let token = localStorage.getItem('token');

// API 请求封装
async function api(endpoint, options = {}) {
    const headers = { 'Content-Type': 'application/json' };
    if (token) headers['Authorization'] = `Bearer ${token}`;
    
    const res = await fetch(`${API_BASE}${endpoint}`, { ...options, headers });
    if (res.status === 401) {
        logout();
        throw new Error('未授权');
    }
    return res.json();
}

// 页面切换
function showPage(pageId) {
    document.querySelectorAll('.page').forEach(p => p.classList.add('hidden'));
    document.getElementById(pageId).classList.remove('hidden');
}

// 登录/注册
document.querySelectorAll('.tab').forEach(tab => {
    tab.addEventListener('click', () => {
        document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
        tab.classList.add('active');
        const isLogin = tab.dataset.tab === 'login';
        document.getElementById('login-form').classList.toggle('hidden', !isLogin);
        document.getElementById('register-form').classList.toggle('hidden', isLogin);
        document.getElementById('auth-error').textContent = '';
    });
});

document.getElementById('login-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    try {
        const res = await api('/user/login', {
            method: 'POST',
            body: JSON.stringify({
                username: document.getElementById('login-username').value,
                password: document.getElementById('login-password').value
            })
        });
        if (res.success) {
            token = res.token;
            localStorage.setItem('token', token);
            showPage('main-page');
            loadGroups();
        } else {
            document.getElementById('auth-error').textContent = '登录失败，请检查用户名和密码';
        }
    } catch (err) {
        document.getElementById('auth-error').textContent = '登录失败';
    }
});

document.getElementById('register-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    try {
        const res = await api('/user/register', {
            method: 'POST',
            body: JSON.stringify({
                username: document.getElementById('register-username').value,
                password: document.getElementById('register-password').value
            })
        });
        if (res.success) {
            token = res.token;
            localStorage.setItem('token', token);
            showPage('main-page');
            loadGroups();
        } else {
            document.getElementById('auth-error').textContent = '注册失败，用户名可能已存在';
        }
    } catch (err) {
        document.getElementById('auth-error').textContent = '注册失败';
    }
});

function logout() {
    token = null;
    localStorage.removeItem('token');
    showPage('auth-page');
}

document.getElementById('logout-btn').addEventListener('click', logout);


// 加载订阅组
async function loadGroups() {
    const res = await api('/subscription/groups');
    const container = document.getElementById('groups-container');
    
    if (!res.success || !res.exportSubGroups?.length) {
        container.innerHTML = '<p style="text-align:center;color:#999;padding:40px;">暂无订阅组，点击上方按钮创建</p>';
        return;
    }
    
    container.innerHTML = res.exportSubGroups.map(g => `
        <div class="group-card" data-id="${g.id}">
            <div class="group-header" onclick="toggleGroup('${g.id}')">
                <div>
                    <h3>${escapeHtml(g.name)}</h3>
                    <div class="group-info">
                        <span>导入: ${g.importSubCount}</span>
                        <span>导出: ${g.exportSubCount}</span>
                        <span class="status-badge ${g.isActive ? 'status-active' : 'status-inactive'}">${g.isActive ? '启用' : '禁用'}</span>
                    </div>
                </div>
                <div class="group-actions" onclick="event.stopPropagation()">
                    <button class="btn-edit" onclick="editGroup('${g.id}', '${escapeHtml(g.name)}', ${g.isActive})">编辑</button>
                    <button class="btn-delete" onclick="deleteGroup('${g.id}')">删除</button>
                </div>
            </div>
            <div class="group-content" id="content-${g.id}"></div>
        </div>
    `).join('');
}

async function toggleGroup(id) {
    const content = document.getElementById(`content-${id}`);
    if (content.classList.contains('expanded')) {
        content.classList.remove('expanded');
        return;
    }
    
    const res = await api(`/subscription/groups/detail?id=${id}`);
    if (!res.success || !res.exportSubGroups?.length) return;
    
    const group = res.exportSubGroups[0];
    content.innerHTML = `
        <div class="sub-section">
            <h4>
                导入订阅
                <button class="btn-add" onclick="addImportSub('${id}')">+ 添加</button>
            </h4>
            <div class="sub-list">
                ${(group.importSubData || []).map(s => `
                    <div class="sub-item">
                        <div class="sub-item-info">
                            <div class="url">${escapeHtml(s.url)}</div>
                            <div class="meta">前缀: ${escapeHtml(s.prefix) || '无'} | <span class="status-badge ${s.isActive ? 'status-active' : 'status-inactive'}">${s.isActive ? '启用' : '禁用'}</span></div>
                        </div>
                        <div class="sub-item-actions">
                            <button class="btn-edit" onclick="editImportSub('${s.id}', '${escapeHtml(s.url)}', '${escapeHtml(s.prefix)}', ${s.isActive})">编辑</button>
                            <button class="btn-delete" onclick="deleteImportSub('${s.id}')">删除</button>
                        </div>
                    </div>
                `).join('') || '<p style="color:#999">暂无导入订阅</p>'}
            </div>
        </div>
        <div class="sub-section">
            <h4>
                导出订阅
                <button class="btn-add" onclick="addExportSub('${id}')">+ 添加</button>
            </h4>
            <div class="sub-list">
                ${(group.exportSubDataList || []).map(s => `
                    <div class="sub-item">
                        <div class="sub-item-info">
                            <div class="url">${location.origin}/sub/${escapeHtml(s.suffix)}</div>
                            <div class="meta">后缀: ${escapeHtml(s.suffix)} | 备注: ${escapeHtml(s.remark) || '无'} | <span class="status-badge ${s.isActive ? 'status-active' : 'status-inactive'}">${s.isActive ? '启用' : '禁用'}</span></div>
                        </div>
                        <div class="sub-item-actions">
                            <button class="btn-copy" onclick="copyUrl('${escapeHtml(s.suffix)}')">复制</button>
                            <button class="btn-edit" onclick="editExportSub('${s.id}', '${escapeHtml(s.suffix)}', '${escapeHtml(s.remark)}', ${s.isActive})">编辑</button>
                            <button class="btn-delete" onclick="deleteExportSub('${s.id}')">删除</button>
                        </div>
                    </div>
                `).join('') || '<p style="color:#999">暂无导出订阅</p>'}
            </div>
        </div>
    `;
    content.classList.add('expanded');
}

function copyUrl(suffix) {
    navigator.clipboard.writeText(`${location.origin}/sub/${suffix}`);
    alert('已复制到剪贴板');
}

function escapeHtml(str) {
    if (!str) return '';
    return str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;').replace(/'/g, '&#039;');
}


// 模态框
function showModal(title, content) {
    document.getElementById('modal-title').textContent = title;
    document.getElementById('modal-body').innerHTML = content;
    document.getElementById('modal').classList.remove('hidden');
}

function hideModal() {
    document.getElementById('modal').classList.add('hidden');
}

document.querySelector('.close-btn').addEventListener('click', hideModal);
document.getElementById('modal').addEventListener('click', (e) => {
    if (e.target.id === 'modal') hideModal();
});

// 订阅组操作
document.getElementById('add-group-btn').addEventListener('click', () => {
    showModal('新建订阅组', `
        <form onsubmit="submitAddGroup(event)">
            <div class="form-group">
                <label>名称</label>
                <input type="text" id="group-name" required>
            </div>
            <div class="checkbox-group">
                <input type="checkbox" id="group-active" checked>
                <label for="group-active">启用</label>
            </div>
            <button type="submit">创建</button>
        </form>
    `);
});

async function submitAddGroup(e) {
    e.preventDefault();
    await api('/subscription/groups', {
        method: 'POST',
        body: JSON.stringify({
            name: document.getElementById('group-name').value,
            isActive: document.getElementById('group-active').checked
        })
    });
    hideModal();
    loadGroups();
}

function editGroup(id, name, isActive) {
    showModal('编辑订阅组', `
        <form onsubmit="submitEditGroup(event, '${id}')">
            <div class="form-group">
                <label>名称</label>
                <input type="text" id="group-name" value="${escapeHtml(name)}" required>
            </div>
            <div class="checkbox-group">
                <input type="checkbox" id="group-active" ${isActive ? 'checked' : ''}>
                <label for="group-active">启用</label>
            </div>
            <button type="submit">保存</button>
        </form>
    `);
}

async function submitEditGroup(e, id) {
    e.preventDefault();
    await api('/subscription/groups', {
        method: 'PUT',
        body: JSON.stringify({
            id,
            name: document.getElementById('group-name').value,
            isActive: document.getElementById('group-active').checked
        })
    });
    hideModal();
    loadGroups();
}

async function deleteGroup(id) {
    if (!confirm('确定删除此订阅组？关联的导入/导出订阅也会被删除')) return;
    await api(`/subscription/groups?id=${id}`, { method: 'DELETE' });
    loadGroups();
}

// 导入订阅操作
function addImportSub(groupId) {
    showModal('添加导入订阅', `
        <form onsubmit="submitAddImportSub(event, '${groupId}')">
            <div class="form-group">
                <label>订阅URL</label>
                <input type="url" id="import-url" required>
            </div>
            <div class="form-group">
                <label>节点前缀</label>
                <input type="text" id="import-prefix" placeholder="可选">
            </div>
            <div class="checkbox-group">
                <input type="checkbox" id="import-active" checked>
                <label for="import-active">启用</label>
            </div>
            <button type="submit">添加</button>
        </form>
    `);
}

async function submitAddImportSub(e, groupId) {
    e.preventDefault();
    await api('/subscription/import-subs', {
        method: 'POST',
        body: JSON.stringify({
            exportSubGroupId: groupId,
            url: document.getElementById('import-url').value,
            prefix: document.getElementById('import-prefix').value,
            isActive: document.getElementById('import-active').checked
        })
    });
    hideModal();
    toggleGroup(groupId);
    toggleGroup(groupId);
}

function editImportSub(id, url, prefix, isActive) {
    showModal('编辑导入订阅', `
        <form onsubmit="submitEditImportSub(event, '${id}')">
            <div class="form-group">
                <label>订阅URL</label>
                <input type="url" id="import-url" value="${escapeHtml(url)}" required>
            </div>
            <div class="form-group">
                <label>节点前缀</label>
                <input type="text" id="import-prefix" value="${escapeHtml(prefix)}">
            </div>
            <div class="checkbox-group">
                <input type="checkbox" id="import-active" ${isActive ? 'checked' : ''}>
                <label for="import-active">启用</label>
            </div>
            <button type="submit">保存</button>
        </form>
    `);
}

async function submitEditImportSub(e, id) {
    e.preventDefault();
    await api('/subscription/import-subs', {
        method: 'PUT',
        body: JSON.stringify({
            id,
            url: document.getElementById('import-url').value,
            prefix: document.getElementById('import-prefix').value,
            isActive: document.getElementById('import-active').checked
        })
    });
    hideModal();
    loadGroups();
}

async function deleteImportSub(id) {
    if (!confirm('确定删除此导入订阅？')) return;
    await api(`/subscription/import-subs?id=${id}`, { method: 'DELETE' });
    loadGroups();
}


// 导出订阅操作
function addExportSub(groupId) {
    showModal('添加导出订阅', `
        <form onsubmit="submitAddExportSub(event, '${groupId}')">
            <div class="form-group">
                <label>后缀 (用于生成订阅链接)</label>
                <input type="text" id="export-suffix" required pattern="[a-zA-Z0-9_-]+" title="只能包含字母、数字、下划线和横线">
            </div>
            <div class="form-group">
                <label>备注</label>
                <input type="text" id="export-remark" placeholder="可选">
            </div>
            <div class="checkbox-group">
                <input type="checkbox" id="export-active" checked>
                <label for="export-active">启用</label>
            </div>
            <button type="submit">添加</button>
        </form>
    `);
}

async function submitAddExportSub(e, groupId) {
    e.preventDefault();
    const res = await api('/subscription/export-subs', {
        method: 'POST',
        body: JSON.stringify({
            exportSubGroupId: groupId,
            suffix: document.getElementById('export-suffix').value,
            remark: document.getElementById('export-remark').value,
            isactive: document.getElementById('export-active').checked
        })
    });
    if (!res.success) {
        alert('添加失败，后缀可能已存在');
        return;
    }
    hideModal();
    toggleGroup(groupId);
    toggleGroup(groupId);
}

function editExportSub(id, suffix, remark, isActive) {
    showModal('编辑导出订阅', `
        <form onsubmit="submitEditExportSub(event, '${id}')">
            <div class="form-group">
                <label>后缀</label>
                <input type="text" id="export-suffix" value="${escapeHtml(suffix)}" required pattern="[a-zA-Z0-9_-]+">
            </div>
            <div class="form-group">
                <label>备注</label>
                <input type="text" id="export-remark" value="${escapeHtml(remark)}">
            </div>
            <div class="checkbox-group">
                <input type="checkbox" id="export-active" ${isActive ? 'checked' : ''}>
                <label for="export-active">启用</label>
            </div>
            <button type="submit">保存</button>
        </form>
    `);
}

async function submitEditExportSub(e, id) {
    e.preventDefault();
    const res = await api('/subscription/export-subs', {
        method: 'PUT',
        body: JSON.stringify({
            id,
            suffix: document.getElementById('export-suffix').value,
            remark: document.getElementById('export-remark').value,
            isactive: document.getElementById('export-active').checked
        })
    });
    if (!res.success) {
        alert('保存失败，后缀可能已存在');
        return;
    }
    hideModal();
    loadGroups();
}

async function deleteExportSub(id) {
    if (!confirm('确定删除此导出订阅？')) return;
    await api(`/subscription/export-subs?id=${id}`, { method: 'DELETE' });
    loadGroups();
}

// 初始化
if (token) {
    showPage('main-page');
    loadGroups();
} else {
    showPage('auth-page');
}
