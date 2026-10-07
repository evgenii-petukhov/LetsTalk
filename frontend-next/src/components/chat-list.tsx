"use client";

import { useChats } from "@/features/chats/queries";
import { useChatStore } from "@/stores/chat-store";
import { formatDate } from "@/lib/format";
import styles from "./chat-list.module.scss";

export function ChatList() {
    const { data, isPending, error } = useChats();
    const selectedChatId = useChatStore((s) => s.selectedChatId);
    const selectChat = useChatStore((s) => s.selectChat);

    if (isPending) return <p className={styles.hint}>Loading chats…</p>;
    if (error) return <p className={styles.hint}>Failed to load chats: {error.message}</p>;
    if (data.length === 0) return <p className={styles.hint}>No chats yet.</p>;

    const sorted = [...data].sort((a, b) => (b.lastMessageDate ?? 0) - (a.lastMessageDate ?? 0));

    return (
        <ul className={styles.list}>
            {sorted.map((chat) => (
                <li key={chat.id}>
                    <button
                        type="button"
                        className={`${styles.item} ${chat.id === selectedChatId ? styles.selected : ""}`}
                        onClick={() => selectChat(chat.id ?? null)}
                    >
                        {chat.photoUrl ? (
                            // eslint-disable-next-line @next/next/no-img-element
                            <img className={styles.avatar} src={chat.photoUrl} alt="" />
                        ) : (
                            <span className={styles.avatar}>
                                {(chat.chatName ?? "?").charAt(0).toUpperCase()}
                            </span>
                        )}
                        <span className={styles.name}>{chat.chatName}</span>
                        <span className={styles.date}>{formatDate(chat.lastMessageDate)}</span>
                        {!!chat.unreadCount && (
                            <span className={styles.badge}>{chat.unreadCount}</span>
                        )}
                    </button>
                </li>
            ))}
        </ul>
    );
}
