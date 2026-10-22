import { ViewportScroller } from '@angular/common';
import { AfterViewInit, Component, ElementRef, inject, OnDestroy, ViewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HomePreview } from './home-preview';
import { HomeDetails } from './home-details';
import { SharedService } from '../../core/services/shared.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, HomePreview, HomeDetails],
  templateUrl: './home.html',
  styleUrl: './home.scss'
})
export class Home implements AfterViewInit, OnDestroy {
  @ViewChild('pageHeader', { static: true }) private pageHeader!: ElementRef<HTMLElement>;

  protected sharedService = inject(SharedService);
  private viewportScroller = inject(ViewportScroller);
  currentYear = new Date().getFullYear();
  company = {
    name: 'INNO'
  };

  ngAfterViewInit(): void {
    // Angular anchor scrolling needs an offset for the responsive sticky header.
    this.viewportScroller.setOffset(() => [0, this.pageHeader.nativeElement.offsetHeight + 24]);
  }

  ngOnDestroy(): void {
    this.viewportScroller.setOffset([0, 0]);
  }
}
