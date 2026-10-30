import { useEffect, useState } from 'react';
import axios from 'axios';
import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, Cell, LineChart, Line, CartesianGrid, Legend } from 'recharts';

interface ActivityData { activityName: string; category: string; durationSec: number; }
interface WeeklyTrend { date: string; Productive: number; Distracting: number; Neutral: number; }
// ДОДАНО: строгий тип для логів, які приходять з бекенду
interface ApiLog { activityName: string; category: string; durationSec: number; startTime: string; } 

const COLORS: Record<string, string> = { 'Productive': '#4caf50', 'Distracting': '#f44336', 'Neutral': '#9e9e9e' };

const MOTIVATIONAL_QUOTES = [
  "Твої цілі самі себе не досягнуть. Закривай вікно і до роботи!",
  "Дисципліна — це вибір між тим, що хочеться зараз, і тим, що справді важливо.",
  "Різниця між тобою і тими, ким ти захоплюєшся — у фокусі.",
  "Час — твій найцінніший ресурс. Не витрачай його на пустий скролінг.",
  "Дофамінова пастка закривається. Повертайся до реальності!"
];

export default function Dashboard() {
  const [dailyData, setDailyData] = useState<ActivityData[]>([]);
  const [weeklyData, setWeeklyData] = useState<WeeklyTrend[]>([]);
  const [warningMessage, setWarningMessage] = useState<string | null>(null);

  const formatTime = (sec: number) => sec >= 60 ? `${Math.floor(sec / 60)} хв ${sec % 60} сек` : `${sec} сек`;

  useEffect(() => {
    const fetchData = () => {
      axios.get('http://localhost:5249/api/telemetry/daily').then(res => {
        // ВИПРАВЛЕНО: замість any використано строгі типи Record<string, ActivityData> та ApiLog
        const grouped = res.data.reduce((acc: Record<string, ActivityData>, log: ApiLog) => {
          if (!acc[log.activityName]) acc[log.activityName] = { activityName: log.activityName, category: log.category || 'Neutral', durationSec: 0 };
          acc[log.activityName].durationSec += log.durationSec;
          return acc;
        }, {});
        
        const chartData: ActivityData[] = Object.values(grouped);
        chartData.sort((a, b) => b.durationSec - a.durationSec);
        setDailyData(chartData);

        const distractingSum = chartData.filter(d => d.category === 'Distracting').reduce((sum, d) => sum + d.durationSec, 0);
        if (distractingSum > 120) {
          if (!warningMessage) setWarningMessage(MOTIVATIONAL_QUOTES[Math.floor(Math.random() * MOTIVATIONAL_QUOTES.length)]);
        } else {
          setWarningMessage(null);
        }
      });

      axios.get('http://localhost:5249/api/telemetry/weekly').then(res => {
        // ВИПРАВЛЕНО: типізація для тижневої статистики
        const groupedByDate = res.data.reduce((acc: Record<string, WeeklyTrend>, log: ApiLog) => {
          const dateStr = new Date(log.startTime).toLocaleDateString();
          if (!acc[dateStr]) acc[dateStr] = { date: dateStr, Productive: 0, Distracting: 0, Neutral: 0 };
          
          // Строга перевірка категорій для TypeScript
          const category = (log.category === 'Productive' || log.category === 'Distracting') ? log.category : 'Neutral';
          acc[dateStr][category] += log.durationSec;
          
          return acc;
        }, {});
        setWeeklyData(Object.values(groupedByDate));
      });
    };

    fetchData();
    const intervalId = setInterval(fetchData, 5000);
    return () => clearInterval(intervalId);
  }, [warningMessage]);

  return (
    <div style={{ padding: 20 }}>
      {warningMessage && (
        <div style={{ backgroundColor: '#ffebee', color: '#c62828', padding: '15px', borderRadius: '8px', marginBottom: '20px', fontWeight: 'bold', borderLeft: '5px solid #f44336', fontSize: '18px' }}>
          ⚠️ Обережно: {warningMessage}
        </div>
      )}

      <h3>Сумарна активність за сьогодні</h3>
      <div style={{ height: 300, width: '100%', marginBottom: 40 }}>
        <ResponsiveContainer>
          <BarChart data={dailyData}>
            <XAxis dataKey="activityName" />
            <YAxis tickFormatter={v => `${Math.floor(v / 60)} хв`} />
            <Tooltip formatter={(v: number) => [formatTime(v), 'Час']} />
            <Bar dataKey="durationSec">{dailyData.map((e, i) => <Cell key={i} fill={COLORS[e.category] || COLORS['Neutral']} />)}</Bar>
          </BarChart>
        </ResponsiveContainer>
      </div>

      <h3>Динаміка за тиждень (хвилин на день)</h3>
      <div style={{ height: 300, width: '100%' }}>
        <ResponsiveContainer>
          <LineChart data={weeklyData}>
            <CartesianGrid strokeDasharray="3 3" opacity={0.2} />
            <XAxis dataKey="date" />
            <YAxis tickFormatter={v => `${Math.floor(v / 60)}`} />
            <Tooltip formatter={(v: number) => [formatTime(v), '']} />
            <Legend />
            <Line type="monotone" dataKey="Productive" name="Продуктивно" stroke={COLORS['Productive']} strokeWidth={3} />
            <Line type="monotone" dataKey="Distracting" name="Відволікання" stroke={COLORS['Distracting']} strokeWidth={3} />
          </LineChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
}