import { getLocalDate } from './date-utils.helper';

describe('getLocalDate', () => {
    it('should convert Unix timestamp to Date object correctly', () => {
        const unixSeconds = 1609459200; // Corresponds to 2021-01-01T00:00:00Z
        const expectedDate = new Date(Date.UTC(2021, 0, 1, 0, 0, 0));

        const result = getLocalDate(unixSeconds);

        expect(result.getTime()).toEqual(expectedDate.getTime());
    });

    it('should handle Unix timestamp of 0 correctly', () => {
        const unixSeconds = 0; // Corresponds to 1970-01-01T00:00:00Z
        const expectedDate = new Date(Date.UTC(1970, 0, 1, 0, 0, 0));

        const result = getLocalDate(unixSeconds);

        expect(result.getTime()).toEqual(expectedDate.getTime());
    });

    it('should handle negative Unix timestamps correctly', () => {
        const unixSeconds = -31536000; // Corresponds to 1969-01-01T00:00:00Z
        const expectedDate = new Date(Date.UTC(1969, 0, 1, 0, 0, 0));

        const result = getLocalDate(unixSeconds);

        expect(result.getTime()).toEqual(expectedDate.getTime());
    });

    it('should handle large Unix timestamps correctly', () => {
        const unixSeconds = 2147483647; // Maximum value for a 32-bit signed integer
        const expectedDate = new Date(Date.UTC(2038, 0, 19, 3, 14, 7));

        const result = getLocalDate(unixSeconds);

        expect(result.getTime()).toEqual(expectedDate.getTime());
    });
});
