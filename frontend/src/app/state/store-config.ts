import { accountsReducer } from './accounts/accounts.reducer';
import { chatsReducer } from './chats/chats.reducer';
import { imageCacheReducer } from './image-cache/image-cache.reducer';
import { loggedInUserReducer } from './logged-in-user/logged-in-user.reducer';
import { messagesReducer } from './messages/messages.reducer';
import { selectedChatInfoReducer } from './selected-chat/selected-chat-info.reducer';
import { videoCallReducer } from './video-call/video-call.reducer';

export const storeConfig = {
    messages: messagesReducer,
    selectedChatInfo: selectedChatInfoReducer,
    chats: chatsReducer,
    accounts: accountsReducer,
    loggedInUser: loggedInUserReducer,
    imageCache: imageCacheReducer,
    videoCall: videoCallReducer,
};
