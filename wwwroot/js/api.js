// Lightweight fetch wrapper. Stores token pair in localStorage and
// retries once with the refresh token on a 401.
const Api = (() => {
    const STORE_ACCESS = 'tb.accessToken';
    const STORE_REFRESH = 'tb.refreshToken';
    const STORE_EMAIL = 'tb.userEmail';

    function getAccess() { return localStorage.getItem(STORE_ACCESS); }
    function getRefresh() { return localStorage.getItem(STORE_REFRESH); }
    function setTokens(access, refresh) {
        localStorage.setItem(STORE_ACCESS, access);
        localStorage.setItem(STORE_REFRESH, refresh);
    }
    function clearTokens() {
        localStorage.removeItem(STORE_ACCESS);
        localStorage.removeItem(STORE_REFRESH);
        localStorage.removeItem(STORE_EMAIL);
    }
    function getEmail() { return localStorage.getItem(STORE_EMAIL); }
    function setEmail(e) { localStorage.setItem(STORE_EMAIL, e); }

    async function request(path, options = {}, isRetry = false) {
        const opts = { ...options };
        opts.headers = { 'Content-Type': 'application/json', ...(opts.headers || {}) };
        const tok = getAccess();
        if (tok) opts.headers['Authorization'] = `Bearer ${tok}`;
        if (opts.body && typeof opts.body !== 'string') opts.body = JSON.stringify(opts.body);

        const res = await fetch(path, opts);

        if (res.status === 401 && !isRetry && getRefresh()) {
            const ok = await tryRefresh();
            if (ok) return request(path, options, true);
        }
        return res;
    }

    async function tryRefresh() {
        const r = await fetch('/api/auth/refresh', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ refreshToken: getRefresh() })
        });
        if (!r.ok) { clearTokens(); return false; }
        const data = await r.json();
        setTokens(data.accessToken, data.refreshToken);
        return true;
    }

    async function login(email, password) {
        const r = await fetch('/api/auth/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password })
        });
        if (!r.ok) {
            const t = await r.text();
            throw new Error(t || 'Identifiants invalides');
        }
        const data = await r.json();
        setTokens(data.accessToken, data.refreshToken);
        setEmail(email);
        return data;
    }

    async function register(username, email, password) {
        const r = await fetch('/api/users/register', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, email, password })
        });
        if (!r.ok) {
            const t = await r.text();
            throw new Error(t || 'Erreur d\'inscription');
        }
        return r.json();
    }

    function logout() {
        clearTokens();
    }

    async function json(path, options) {
        const r = await request(path, options);
        if (!r.ok) {
            const t = await r.text();
            throw new Error(`${r.status} ${t}`);
        }
        if (r.status === 204) return null;
        const text = await r.text();
        return text ? JSON.parse(text) : null;
    }

    return {
        login, register, logout,
        getAccess, getEmail, isAuthenticated: () => !!getAccess(),
        // CRUD helpers
        getWorkspaces: () => json('/api/workspaces'),
        createWorkspace: (name, memberEmails) => json('/api/workspaces', { method: 'POST', body: { name, memberEmails } }),
        deleteWorkspace: id => json(`/api/workspaces/${id}`, { method: 'DELETE' }),
        getWorkspaceMembers: id => json(`/api/workspaces/${id}/members`),
        inviteWorkspaceMember: (id, email) => json(`/api/workspaces/${id}/members`, { method: 'POST', body: { email } }),
        removeWorkspaceMember: (id, userId) => json(`/api/workspaces/${id}/members/${userId}`, { method: 'DELETE' }),

        getBoards: () => json('/api/boards'),
        createBoard: (name, workspaceId) => json('/api/boards', { method: 'POST', body: { name, workspaceId } }),
        deleteBoard: id => json(`/api/boards/${id}`, { method: 'DELETE' }),

        getLists: boardId => json(`/api/lists/board/${boardId}`),
        createList: (name, position, boardId) => json('/api/lists', { method: 'POST', body: { name, position, boardId } }),
        deleteList: id => json(`/api/lists/${id}`, { method: 'DELETE' }),

        getCards: listId => json(`/api/cards/list/${listId}`),
        getCard: id => json(`/api/cards/${id}`),
        createCard: (title, position, listId, dueDate) =>
            json('/api/cards', { method: 'POST', body: { title, description: null, position, dueDate, listId } }),
        updateCard: (id, payload) => json(`/api/cards/${id}`, { method: 'PUT', body: payload }),
        deleteCard: id => json(`/api/cards/${id}`, { method: 'DELETE' }),

        getComments: cardId => json(`/api/cards/${cardId}/comments`),
        createComment: (cardId, content) => json(`/api/cards/${cardId}/comments`, { method: 'POST', body: { content } }),
        deleteComment: id => json(`/api/comments/${id}`, { method: 'DELETE' }),
    };
})();

