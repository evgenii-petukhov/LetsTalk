export const getLocalDate = (unixSeconds: number): Date => {
    const date = new Date(0);
    date.setUTCSeconds(unixSeconds);
    return date;
};
