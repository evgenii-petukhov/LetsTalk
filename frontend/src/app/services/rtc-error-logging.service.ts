import { inject, Injectable } from '@angular/core';
import { RtcPeerConnectionManager } from './rtc-peer-connection-manager';
import { Store } from '@ngrx/store';
import { firstValueFrom } from 'rxjs';
import { ApiService } from './api.service';
import { selectVideoCall } from '../state/video-call/video-call.selectors';
import { RtcErrorType } from '../api-client/api-client';
import { DebugService } from './debug.service';

@Injectable({
    providedIn: 'root',
})
export class RtcErrorLoggingService {
    private readonly connectionManager = inject(RtcPeerConnectionManager);
    private readonly store = inject(Store);
    private readonly apiService = inject(ApiService);
    private readonly debugService = inject(DebugService);

    logConnectionError(errorMessage: string, error?: any): Promise<void> {
        return this.logError(RtcErrorType.Connection, errorMessage, error);
    }

    logIceServerError(errorMessage: string, error?: any): Promise<void> {
        return this.logError(RtcErrorType.IceServer, errorMessage, error);
    }

    logMediaStreamError(errorMessage: string, error?: any): Promise<void> {
        return this.logError(RtcErrorType.Media, errorMessage, error);
    }

    private async logError(
        errorType: RtcErrorType,
        errorMessage: string,
        error?: any,
    ): Promise<void> {
        const callSettings = await firstValueFrom(
            this.store.select(selectVideoCall),
        );
        if (callSettings.callId) {
            const diagnostics = await this.connectionManager.getDiagnostics();
            await this.apiService.logWebRtcError(
                callSettings.callId,
                callSettings.chatId,
                diagnostics,
                errorType,
                errorMessage || error,
                this.debugService.getStackTrace(error),
            );
        }
    }
}
