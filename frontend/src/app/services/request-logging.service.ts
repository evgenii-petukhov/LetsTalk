import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root',
})
export class RequestLoggingService {
    log(
        protocol: string,
        url: string,
        time: number,
        downloaded: number,
        uploaded: number,
    ): void {
        console.log(
            `[${protocol}] ${url} ${time.toFixed()}ms${this.getDownloadString(downloaded)}${this.getUploadString(uploaded)}`,
        );
    }

    private getDownloadString(volume: number) {
        return this.getDataVolumeString(volume, '▼');
    }

    private getUploadString(volume: number) {
        return this.getDataVolumeString(volume, '▲');
    }

    private getDataVolumeString(volume: number, icon: string) {
        return volume > 0 ? ` ${icon}${Math.ceil(volume / 1024)}kB` : '';
    }
}
