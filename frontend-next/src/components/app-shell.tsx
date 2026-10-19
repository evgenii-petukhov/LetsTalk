"use client";

import { useSyncExternalStore } from "react";
import { TOKEN_KEY } from "@/lib/api/client";
import { ChatList } from "./chat-list";
import { MessageList } from "./message-list";
import styles from "./app-shell.module.scss";

const subscribe = (cb: () => void) => {
    window.addEventListener("storage", cb);
    return () => window.removeEventListener("storage", cb);
};
const hasToken = () => !!window.localStorage.getItem(TOKEN_KEY);

export function AppShell() {
    const authorized = useSyncExternalStore(subscribe, hasToken, () => null);

    if (authorized === null) return null;
    if (!authorized) {
        return (
            <p className={styles.hint}>
                No token found. Log in via the Angular app, then run{" "}
                <code>
                    localStorage.setItem(&quot;{TOKEN_KEY}&quot;, &quot;&lt;token&gt;&quot;)
                </code>{" "}
                in this origin&apos;s browser console and reload.
            </p>
        );
    }

    return (
        <div className={styles.shell}>
            <aside className={styles.sidebar}>
                <ChatList />
            </aside>
            <main className={styles.main}>
                <MessageList />
            </main>
        </div>
    );
}
