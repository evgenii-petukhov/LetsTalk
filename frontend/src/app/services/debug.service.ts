import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root',
})
export class DebugService {
    getStackTrace(error?: Error): string {
        if (error && error.stack) {
            return error.stack;
        }
        
        // Fallback: create new error to get stack
        const err = new Error();
        return err.stack || 'No stack trace available';
    }
}
