/* eslint-disable @typescript-eslint/no-explicit-any */
import { inject, Injectable } from '@angular/core';
import { ToastrService } from 'ngx-toastr';

@Injectable({
    providedIn: 'root',
})
export class ErrorService {
    private readonly toastr = inject(ToastrService);

    handleError(e: any, defaultMessage: string): void {
        const errors = this.getCommaSeparatedErrorMessages(e, defaultMessage);
        this.toastr.error(errors, 'Error');
    }

    private getCommaSeparatedErrorMessages(
        e: any,
        defaultMessage: string,
    ): string {
        const response = JSON.parse(e?.response || '{}');
        return (
            Object.entries(response.errors || {})
                .map((x) => x[1] as string)
                .flat()
                .join(', ') || defaultMessage
        );
    }
}
