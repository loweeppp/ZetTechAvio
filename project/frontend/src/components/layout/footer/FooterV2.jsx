import React from 'react';
import { Plane } from 'lucide-react';
import { Link } from 'react-router-dom';
import { useTranslation } from '../../../i18n/TranslationProvider';
import './FooterV2.css';

export default function FooterV2() {
  const { t } = useTranslation();
  const columns = [
    { title: t('footer.company'), links: [t('footer.about')] },
    { title: t('footer.support'), links: [t('footer.contacts'), t('footer.privacy'), t('footer.terms')] },
  ];

  return (
    <footer className="footerv2">
      <div className="footerv2__container">
        <div className="footerv2__grid">
          <div className="footerv2__brand">
            <div className="footerv2__logo">
              <div className="">
                <Link to="/" className="headerv2__logo">
                  <span className="headerv2__logo_icon">
                    <img src="/routing-2.ico" className="headerv2__imagelogo" />
                  </span>
                  <span className="headerv2__name">
                    Zet<span className="headerv2__nameAccent">Tech</span>Avio
                  </span>
                </Link>
              </div>
            </div>
            <p className="footerv2__desc">{t('footer.description')}</p>
          </div>

          {columns.map((col) => (
            <div key={col.title}>
              <div className="footerv2__title">{col.title}</div>
              <ul className="footerv2__links">
                {col.links.map((l) => (
                  <li key={l}>
                    <a href="/" onClick={(e) => e.preventDefault()} className="footerv2__link">
                      {l}
                    </a>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>

        <div className="footerv2__bottom">
          <div>© {new Date().getFullYear()} ZetTechAvio. {t('footer.copyright')}</div>
          <div className="footerv2__bottomLinks">
            <a href="/privacy" target="_blank" className="footerv2__bottomLink">
              {t('footer.cookies')}
            </a>
            <a href="/privacy" target="_blank" className="footerv2__bottomLink">
              {t('footer.privacy')}
            </a>
            <a href="/privacy" target="_blank" className="footerv2__bottomLink">
              {t('footer.terms')}
            </a>
          </div>
        </div>
      </div>
    </footer>
  );
}

