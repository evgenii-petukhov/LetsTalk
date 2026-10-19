import { create } from "zustand";

interface ChatState {
    selectedChatId: string | null;
    selectChat: (id: string | null) => void;
}

export const useChatStore = create<ChatState>((set) => ({
    selectedChatId: null,
    selectChat: (id) => set({ selectedChatId: id }),
}));
