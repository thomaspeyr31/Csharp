// SignalR client. One connection per session, joined to the active board group.
const Realtime = (() => {
    let connection = null;
    let currentBoardId = null;

    async function ensureConnected() {
        if (connection && connection.state === signalR.HubConnectionState.Connected) return connection;
        if (connection) {
            try { await connection.stop(); } catch (_) {}
        }
        connection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/board', {
                accessTokenFactory: () => Api.getAccess()
            })
            .withAutomaticReconnect()
            .build();

        ['CardCreated', 'CardUpdated', 'CardDeleted',
         'ListCreated', 'ListDeleted',
         'CommentCreated', 'CommentUpdated', 'CommentDeleted'
        ].forEach(evt => connection.on(evt, _ => Board.onRemoteChange()));

        connection.onreconnected(async () => {
            if (currentBoardId !== null) {
                await connection.invoke('JoinBoard', currentBoardId);
            }
        });

        await connection.start();
        return connection;
    }

    async function connectToBoard(boardId) {
        const conn = await ensureConnected();
        if (currentBoardId !== null && currentBoardId !== boardId) {
            try { await conn.invoke('LeaveBoard', currentBoardId); } catch (_) {}
        }
        currentBoardId = boardId;
        await conn.invoke('JoinBoard', boardId);
    }

    async function disconnect() {
        if (!connection) return;
        try { await connection.stop(); } catch (_) {}
        connection = null;
        currentBoardId = null;
    }

    return { connectToBoard, disconnect };
})();

