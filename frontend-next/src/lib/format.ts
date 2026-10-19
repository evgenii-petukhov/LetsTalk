// Backend timestamps are Unix time in seconds.
export const formatTime = (unix?: number) =>
    unix
        ? new Date(unix * 1000).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })
        : "";

export const formatDate = (unix?: number) =>
    unix ? new Date(unix * 1000).toLocaleDateString() : "";
