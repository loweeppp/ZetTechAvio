import React from 'react';
import { Plane } from 'lucide-react';
import './FooterV2.css';

export default function FooterV2() {
  return (
    <footer className="footerv2">
      <div className="footerv2__container">
        <div className="footerv2__grid">
          <div className="footerv2__brand">
            <div className="footerv2__logo">
              <div className="footerv2__mark">
                <Plane className="footerv2__plane" strokeWidth={2.5} />
              </div>
              <span className="footerv2__name">
                Zet<span className="footerv2__nameAccent">Tech</span>Avio
              </span>
            </div>
            <p className="footerv2__desc">
            Веб-сервис для поиска и покупки авиабилетов. Форма поиска представляет формы
                            поиска по городу отправления/прибытия и дате, позволяет сразу рейсов, оформить покупку и
                            получить электронный билет.
            </p>
          </div>

          {[
            { title: 'Компания', links: ['О нас', 'Карьера', 'Пресса', 'Блог'] },
            {
              title: 'Поддержка',
              links: ['Центр помощи', 'Контакты', 'Конфиденциальность', 'Условия'],
            },
          ].map((col) => (
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
          <div>© 2026 ZetTechAvio. Все права защищены.</div>
          <div className="footerv2__bottomLinks">
            <a href="/" onClick={(e) => e.preventDefault()} className="footerv2__bottomLink">
              Cookies
            </a>
            <a href="/" onClick={(e) => e.preventDefault()} className="footerv2__bottomLink">
              Конфиденциальность
            </a>
            <a href="/" onClick={(e) => e.preventDefault()} className="footerv2__bottomLink">
              Условия
            </a>
          </div>
        </div>
      </div>
    </footer>
  );
}

