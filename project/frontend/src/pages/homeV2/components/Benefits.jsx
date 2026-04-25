import React from 'react';
import { ShieldCheck, Sparkles, Headphones, BadgePercent } from 'lucide-react';

const ITEMS = [
  {
    Icon: BadgePercent,
    title: 'Никаких скрытых комиссий',
    text: 'Цена, которую вы видите — это цена, которую вы платите. Каждая статья расходов чётко указана.',
  },
  {
    Icon: Sparkles,
    title: 'Быстрое бронирование',
    text: 'Забронируйте менее чем за минуту с умным автозаполнением и сохранёнными профилями путешественников.',
  },
  {
    Icon: Headphones,
    title: 'Поддержка 24/7',
    text: 'Живые люди в любой точке мира, готовые помочь при изменении планов.',
  },
  {
    Icon: ShieldCheck,
    title: 'Безопасные платежи',
    text: 'Шифрование банковского уровня и защита от мошенничества при каждой транзакции.',
  },
];

export default function Benefits() {
  return (
    <section className="homev2__benefits">
      <div className="homev2__container">
        <div className="homev2__sectionHead homev2__sectionHead--benefits">
          <div className="homev2__kicker">Почему ZetTechAvio</div>
          <h2 className="homev2__h2">Создано для вашего путешествия</h2>
        </div>

        <div className="homev2__benefitsGrid">
          {ITEMS.map((it) => (
            <div key={it.title} className="homev2__benefitCard">
              <div className="homev2__benefitIcon">
                <it.Icon className="homev2__benefitSvg" />
              </div>
              <div className="homev2__benefitTitle">{it.title}</div>
              <p className="homev2__benefitText">{it.text}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}

